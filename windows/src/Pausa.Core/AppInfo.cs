namespace Pausa.Core;

public static class AppInfo
{
    /// <summary>Short display version, e.g. "1.0" or "1.0.1" (matches the macOS "1.0" style).</summary>
    public static string Version { get; } = FormatVersion(typeof(AppInfo).Assembly.GetName().Version);

    public static string FormatVersion(Version? version)
    {
        if (version is null) return "0.0";
        return version.Build > 0
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : $"{version.Major}.{version.Minor}";
    }
}
