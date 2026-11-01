namespace Pausa.Core.Tests;

public class UpdateCheckerTests
{
    private static readonly Version Win11 = new(10, 0, 26100);

    private static string Latest(string version, string? minOs = "10.0.17763", string platform = "windows",
        string? updateAvailable = null, string notes = "https://github.com/seriamo/pausa/releases/tag/windows-v1.1") =>
        $$"""
        {
          "product": "pausa", "channel": "stable", "platform": "{{platform}}", "version": "{{version}}",
          "notes_url": "{{notes}}", "download_url": "https://example.com/Pausa-Setup-x64.exe",
          "min_os_version": {{(minOs is null ? "null" : $"\"{minOs}\"")}}, "assets": []
          {{(updateAvailable is null ? "" : $", \"update_available\": {updateAvailable}")}}
        }
        """;

    [Fact]
    public void Reports_a_newer_release()
    {
        var update = UpdateChecker.FromLatest(Latest("1.1", updateAvailable: "true"), "1.0.0", Win11);
        Assert.NotNull(update);
        Assert.Equal("1.1", update!.Version);
        Assert.Equal(new Uri("https://github.com/seriamo/pausa/releases/tag/windows-v1.1"), update.ReleaseUrl);
    }

    [Fact]
    public void Ignores_same_older_and_server_says_no()
    {
        Assert.Null(UpdateChecker.FromLatest(Latest("1.0"), "1.0.0", Win11));
        Assert.Null(UpdateChecker.FromLatest(Latest("0.9"), "1.0", Win11));
        Assert.Null(UpdateChecker.FromLatest(Latest("1.1", updateAvailable: "false"), "1.0", Win11));
    }

    [Fact]
    public void Ignores_releases_this_windows_cannot_run_or_other_platforms()
    {
        Assert.Null(UpdateChecker.FromLatest(Latest("1.1", minOs: "10.0.99999"), "1.0", Win11));
        Assert.NotNull(UpdateChecker.FromLatest(Latest("1.1", minOs: null), "1.0", Win11));
        Assert.Null(UpdateChecker.FromLatest(Latest("2.0", platform: "macos"), "1.0", Win11));
    }

    [Fact]
    public void Falls_back_to_releases_page_for_unsafe_notes_links()
    {
        var update = UpdateChecker.FromLatest(Latest("1.1", notes: "http://example.com/notes"), "1.0", Win11);
        Assert.Equal(Endpoints.ReleasesPage, update!.ReleaseUrl);
    }

    [Fact]
    public void Returns_null_for_garbage()
    {
        Assert.Null(UpdateChecker.FromLatest("[]", "1.0", Win11));
        Assert.Null(UpdateChecker.FromLatest("""{"error":"no_releases"}""", "1.0", Win11));
        Assert.Null(UpdateChecker.FromLatest("""{"version": "1.1 beta"}""", "1.0", Win11));
    }

    [Theory]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("1.0.1", "1.0", 1)]
    [InlineData("1.9", "1.10", -1)]
    [InlineData("2", "1.99.99", 1)]
    public void Compares_versions_numerically(string a, string b, int sign) =>
        Assert.Equal(sign, Math.Sign(UpdateChecker.CompareVersions(a, b)));
}
