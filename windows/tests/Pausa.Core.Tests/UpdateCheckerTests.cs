namespace Pausa.Core.Tests;

public class UpdateCheckerTests
{
    private const string Releases = """
    [
      { "tag_name": "windows-v1.2", "draft": true, "prerelease": false, "html_url": "https://example.com/draft" },
      { "tag_name": "windows-v1.1.0-beta", "draft": false, "prerelease": false, "html_url": "https://example.com/beta" },
      { "tag_name": "windows-v1.3", "draft": false, "prerelease": true, "html_url": "https://example.com/pre" },
      { "tag_name": "v2.0", "draft": false, "prerelease": false, "html_url": "https://example.com/mac" },
      { "tag_name": "Windows-v1.0.1", "draft": false, "prerelease": false, "html_url": "https://example.com/w101" },
      { "tag_name": "windows-v1.0", "draft": false, "prerelease": false, "html_url": "https://example.com/w10" }
    ]
    """;

    [Fact]
    public void Picks_newest_published_windows_release()
    {
        var update = UpdateChecker.FindNewer(Releases, "1.0");
        Assert.NotNull(update);
        Assert.Equal("1.0.1", update!.Version);
        Assert.Equal(new Uri("https://example.com/w101"), update.ReleaseUrl);
    }

    [Fact]
    public void Ignores_macos_tags_drafts_and_prereleases()
    {
        Assert.Null(UpdateChecker.FindNewer(Releases, "1.0.1"));
    }

    [Fact]
    public void Returns_null_for_garbage()
    {
        Assert.Null(UpdateChecker.FindNewer("{}", "1.0"));
        Assert.Null(UpdateChecker.FindNewer("[]", "1.0"));
        Assert.Null(UpdateChecker.FindNewer("""[{"tag_name": 5}]""", "1.0"));
    }

    [Theory]
    [InlineData("1.0", "1.0.0", 0)]
    [InlineData("1.0.1", "1.0", 1)]
    [InlineData("1.9", "1.10", -1)]
    [InlineData("2", "1.99.99", 1)]
    public void Compares_versions_numerically(string a, string b, int sign) =>
        Assert.Equal(sign, Math.Sign(UpdateChecker.CompareVersions(a, b)));
}
