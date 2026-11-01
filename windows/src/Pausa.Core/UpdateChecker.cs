using System.Text.Json;

namespace Pausa.Core;

public sealed record AvailableUpdate(string Version, Uri ReleaseUrl);

/// <summary>
/// Asks updates.seriamo.com for the newest Windows release. Downloads stay on GitHub Releases
/// (Windows tags are windows-v*; the macOS app owns the repo's "latest" release).
/// </summary>
public static class UpdateChecker
{
    /// <summary>Parses a /releases/latest response and returns it if it's newer and supports this OS.</summary>
    public static AvailableUpdate? FromLatest(string json, string currentVersion, Version osVersion)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object) return null;
        if (String(root, "platform") is { } platform && platform != Endpoints.Platform) return null;

        var version = String(root, "version");
        if (version is null || !IsNumericVersion(version)) return null;

        var newer = root.TryGetProperty("update_available", out var flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? flag.GetBoolean()
            : CompareVersions(version, currentVersion) > 0;
        if (!newer || CompareVersions(version, currentVersion) <= 0) return null;
        if (!Supports(String(root, "min_os_version"), osVersion)) return null;

        var page = Uri.TryCreate(String(root, "notes_url"), UriKind.Absolute, out var notes) && notes.Scheme == Uri.UriSchemeHttps
            ? notes
            : Endpoints.ReleasesPage;
        return new AvailableUpdate(version, page);
    }

    public static async Task<AvailableUpdate?> CheckAsync(HttpClient http, string currentVersion, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.LatestRelease(currentVersion));
            request.Headers.Accept.ParseAdd("application/json");
            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return FromLatest(json, currentVersion, Environment.OSVersion.Version);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    /// <summary>A missing or unreadable minimum never hides an update.</summary>
    public static bool Supports(string? minimum, Version osVersion) =>
        string.IsNullOrWhiteSpace(minimum) || !System.Version.TryParse(minimum, out var required) || osVersion >= required;

    /// <summary>Numeric, segment-wise comparison where missing segments count as zero ("1.0" == "1.0.0").</summary>
    public static int CompareVersions(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');
        for (var i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            var x = i < pa.Length && int.TryParse(pa[i], out var va) ? va : 0;
            var y = i < pb.Length && int.TryParse(pb[i], out var vb) ? vb : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }

    private static bool IsNumericVersion(string version) =>
        version.Length > 0 && version.Split('.').All(part => part.Length > 0 && part.All(char.IsAsciiDigit));

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
