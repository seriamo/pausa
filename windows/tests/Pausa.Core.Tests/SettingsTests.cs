using System.Text.Json;

namespace Pausa.Core.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pausa-tests-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "nested", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Missing_file_yields_macos_defaults()
    {
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(1200, s.IntervalSeconds);
        Assert.Equal(0.4, s.Intensity);
        Assert.Equal(20, s.DurationSeconds);
        Assert.Equal(FlashMode.TopGlow, s.FlashMode);
        Assert.Equal("Red", s.FlashColor);
        Assert.False(s.ActiveScreenOnly);
        Assert.True(s.PauseDuringFullscreen);
        Assert.True(s.ShareUsage);
        Assert.False(s.HasLaunchedBefore);
    }

    [Fact]
    public void Round_trips_and_creates_directory()
    {
        var store = new SettingsStore(FilePath);
        var s = store.Load();
        s.IntervalSeconds = 2700;
        s.Intensity = 0.7;
        s.DurationSeconds = 7;
        s.FlashMode = FlashMode.ScreenBorder;
        s.FlashColor = "Blue";
        s.ActiveScreenOnly = true;
        s.PauseDuringFullscreen = false;
        s.HasLaunchedBefore = true;
        store.Save(s);

        var json = File.ReadAllText(FilePath);
        Assert.Contains("\"screenBorder\"", json);

        var loaded = new SettingsStore(FilePath).Load();
        Assert.Equal(2700, loaded.IntervalSeconds);
        Assert.Equal(0.7, loaded.Intensity);
        Assert.Equal(7, loaded.DurationSeconds);
        Assert.Equal(FlashMode.ScreenBorder, loaded.FlashMode);
        Assert.Equal("Blue", loaded.FlashColor);
        Assert.True(loaded.ActiveScreenOnly);
        Assert.False(loaded.PauseDuringFullscreen);
        Assert.True(loaded.HasLaunchedBefore);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ not json");
        Assert.Equal(1200, new SettingsStore(FilePath).Load().IntervalSeconds);
    }

    [Fact]
    public void Out_of_range_values_snap_to_steps()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, """
            { "intervalSeconds": 1000, "intensity": 0.9, "durationSeconds": 11, "flashColor": "magenta" }
            """);
        var s = new SettingsStore(FilePath).Load();
        Assert.Equal(900, s.IntervalSeconds);
        Assert.Equal(0.7, s.Intensity);
        Assert.Equal(10, s.DurationSeconds);
        Assert.Equal("Red", s.FlashColor);
    }

    [Fact]
    public void Device_id_is_created_once_and_uppercase()
    {
        var store = new SettingsStore(FilePath);
        var s = store.Load();
        var id = store.EnsureDeviceId(s);
        Assert.True(Guid.TryParse(id, out _));
        Assert.Equal(id.ToUpperInvariant(), id);
        Assert.Equal(id, store.EnsureDeviceId(new SettingsStore(FilePath).Load()));
    }

    [Theory]
    [InlineData(1, 0, 0, "1.0")]
    [InlineData(1, 2, 0, "1.2")]
    [InlineData(1, 0, 3, "1.0.3")]
    public void Formats_display_version(int major, int minor, int build, string expected) =>
        Assert.Equal(expected, AppInfo.FormatVersion(new Version(major, minor, build)));

    [Fact]
    public void Turning_usage_off_forgets_the_install_id()
    {
        var store = new SettingsStore(FilePath);
        var s = store.Load();
        var first = store.EnsureDeviceId(s);
        s.ShareUsage = false;
        store.ResetDeviceId(s);

        var reloaded = store.Load();
        Assert.False(reloaded.ShareUsage);
        Assert.Null(reloaded.DeviceId);
        Assert.NotEqual(first, store.EnsureDeviceId(reloaded));
    }
}
