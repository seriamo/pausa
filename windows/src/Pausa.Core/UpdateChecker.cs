using System.Net.Http.Headers;
using System.Text.Json;

namespace Pausa.Core;

public sealed record AvailableUpdate(string Version, Uri ReleaseUrl);

/// <summary>
/// Finds the newest published Windows release. macOS and Windows share the GitHub repo, so
/// Windows releases are tagged windows-v* and are never marked "latest" (the macOS app reads
/// /releases/latest).
/// </summary>
public static class UpdateChecker
{
    public const string TagPrefix = "windows-v";

    public static AvailableUpdate? FindNewer(string releasesJson, string currentVersion)
    {
        using var doc = JsonDocument.Parse(releasesJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

        AvailableUpdate? best = null;
        foreach (var release in doc.RootElement.EnumerateArray())
        {
            if (IsTrue(release, "draft") || IsTrue(release, "prerelease")) continue;
            if (release.ValueKind != JsonValueKind.Object) continue;
            if (!release.TryGetProperty("tag_name", out var tagProp) || tagProp.ValueKind != JsonValueKind.String) continue;
            var tag = tagProp.GetString()!;
            if (!tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)) continue;

            var version = tag[TagPrefix.Length..];
            if (!IsNumericVersion(version)) continue;

            var url = release.TryGetProperty("html_url", out var urlProp)
                      && urlProp.ValueKind == JsonValueKind.String
                      && Uri.TryCreate(urlProp.GetString(), UriKind.Absolute, out var parsed)
                ? parsed
                : Endpoints.ReleasesPage;

            if (best is null || CompareVersions(version, best.Version) > 0) best = new AvailableUpdate(version, url);
        }

        return best is not null && CompareVersions(best.Version, currentVersion) > 0 ? best : null;
    }

    public static async Task<AvailableUpdate?> CheckAsync(HttpClient http, string currentVersion, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Releases);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            // GitHub's API rejects requests without a User-Agent; HttpClient doesn't send one by default.
            if (http.DefaultRequestHeaders.UserAgent.Count == 0) request.Headers.UserAgent.ParseAdd($"Pausa-Windows/{currentVersion}");
            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return FindNewer(json, currentVersion);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

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

    private static bool IsTrue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
}
