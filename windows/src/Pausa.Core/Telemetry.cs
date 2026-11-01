using System.Net;
using System.Text;
using System.Text.Json;

namespace Pausa.Core;

/// <summary>
/// Anonymous usage telemetry: one <c>app_launch</c> event per launch with the app version and a
/// random install ID. Nothing else. Turning "Share anonymous usage" off stops sending and deletes
/// the install ID.
/// </summary>
public static class Telemetry
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    public static string BuildLaunchEnvelope(string version, string installId, DateTimeOffset now, Guid eventId) =>
        JsonSerializer.Serialize(new
        {
            schema_version = 1,
            events = new[]
            {
                new Dictionary<string, string>
                {
                    ["event_id"] = eventId.ToString(),
                    ["product"] = Endpoints.Product,
                    ["event"] = "app_launch",
                    ["app_version"] = version,
                    ["platform"] = Endpoints.Platform,
                    ["occurred_at"] = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"),
                    ["install_id"] = installId,
                },
            },
        });

    /// <summary>Network errors, 429 and 5xx are worth one more try; anything else is final.</summary>
    public static bool IsRetryable(HttpStatusCode? status) =>
        status is null || status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    /// <summary>
    /// Fire and forget. Retries once after 30 s; the event ID stays the same, so the server
    /// drops a duplicate if the first attempt did land.
    /// </summary>
    public static async Task SendLaunchAsync(HttpClient http, string version, string installId, CancellationToken ct = default)
    {
        var body = BuildLaunchEnvelope(version, installId, DateTimeOffset.UtcNow, Guid.NewGuid());
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (attempt > 0)
            {
                try { await Task.Delay(RetryDelay, ct).ConfigureAwait(false); }
                catch (TaskCanceledException) { return; }
            }

            HttpStatusCode? status = null;
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                using var response = await http.PostAsync(Endpoints.Telemetry, content, ct).ConfigureAwait(false);
                status = response.StatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (ct.IsCancellationRequested) return;
            }

            if (!IsRetryable(status)) return;
        }
    }
}
