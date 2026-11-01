namespace Pausa.Core;

/// <summary>
/// Every network address the app talks to lives here. All Seriamo apps share the same three
/// hosts (telemetry, updates, feedback), so the privacy policy has one place to describe them.
/// </summary>
public static class Endpoints
{
    public const string Product = "pausa";
    public const string Platform = "windows";

    public static readonly Uri Telemetry = new("https://telemetry.seriamo.com/v1/events");

    public static Uri LatestRelease(string currentVersion) =>
        new($"https://updates.seriamo.com/v1/products/{Product}/releases/latest" +
            $"?current={Uri.EscapeDataString(currentVersion)}&platform={Platform}");

    public static Uri FeedbackForm(string version) =>
        new($"https://feedback.seriamo.com/?product={Product}&version={Uri.EscapeDataString(version)}&platform={Platform}");

    public static readonly Uri ReleasesPage = new("https://github.com/seriamo/pausa/releases");

    public static readonly Uri Website = new("https://seriamo.com");
}
