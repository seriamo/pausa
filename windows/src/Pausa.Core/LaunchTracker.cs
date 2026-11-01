using System.Text;
using System.Text.Json;

namespace Pausa.Core;

/// <summary>Anonymous launch ping: app version, a random device ID and the platform. Nothing else.</summary>
public static class LaunchTracker
{
    public const string Platform = "windows";

    public static string BuildPayload(string version, string deviceId) =>
        JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["version"] = version,
            ["device_id"] = deviceId,
            ["platform"] = Platform,
        });

    public static async Task SendAsync(HttpClient http, string version, string deviceId, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.Launch)
            {
                Content = new StringContent(BuildPayload(version, deviceId), Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("x-launch-token", Endpoints.LaunchToken);
            using var _ = await http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Fire and forget, like the macOS app.
        }
    }
}
