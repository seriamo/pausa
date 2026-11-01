namespace Pausa.Core;

/// <summary>
/// Every network address the app talks to lives here, so moving to new
/// update or telemetry endpoints is a one-file change.
/// </summary>
public static class Endpoints
{
    public static readonly Uri Launch = new("https://api.seriamo.com/pausa/launch");

    // Same shared client token as the macOS app. Velvet rope, not a vault door.
    public const string LaunchToken = "Q7cDfnufc3DKZSW7fkcSsgvb2dIsWtFT-upIM9GbF0s";

    /// <summary>Recent releases (macOS and Windows share the repo; Windows tags are windows-v*).</summary>
    public static readonly Uri Releases = new("https://api.github.com/repos/seriamo/pausa/releases?per_page=30");

    public static readonly Uri ReleasesPage = new("https://github.com/seriamo/pausa/releases");

    public static readonly Uri Website = new("https://seriamo.com");
}
