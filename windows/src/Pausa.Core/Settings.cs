using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pausa.Core;

/// <summary>All persisted settings. Launch-at-login lives in the registry, not here.</summary>
public sealed class PausaSettings
{
    public int IntervalSeconds { get; set; } = 20 * 60;
    public double Intensity { get; set; } = 0.4;
    public double DurationSeconds { get; set; } = 20;
    public FlashMode FlashMode { get; set; } = FlashMode.TopGlow;
    public string FlashColor { get; set; } = Core.FlashColor.Default.Name;
    public bool ActiveScreenOnly { get; set; }
    public bool PauseDuringFullscreen { get; set; } = true;
    /// <summary>Anonymous install ID for the launch count. Deleted when usage sharing is turned off.</summary>
    public string? DeviceId { get; set; }
    public bool ShareUsage { get; set; } = true;
    public bool HasLaunchedBefore { get; set; }

    /// <summary>Snaps every value onto a valid slider stop so hand-edited files can't break the UI.</summary>
    public void Normalize()
    {
        IntervalSeconds = Steps.IntervalSeconds[Steps.NearestIndex(Steps.IntervalSeconds, IntervalSeconds)];
        Intensity = Steps.Intensity[Steps.NearestIndex(Steps.Intensity, Intensity)].Value;
        DurationSeconds = Steps.Duration[Steps.NearestIndex(Steps.Duration, DurationSeconds)].Value;
        if (!Enum.IsDefined(FlashMode)) FlashMode = FlashMode.TopGlow;
        FlashColor = Core.FlashColor.FromName(FlashColor).Name;
    }
}

/// <summary>JSON settings file, by default %APPDATA%\Seriamo\Pausa\settings.json.</summary>
public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    public string Path { get; } = path;

    public static string DefaultPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Seriamo", "Pausa", "settings.json");

    public PausaSettings Load()
    {
        PausaSettings settings;
        try
        {
            settings = File.Exists(Path)
                ? JsonSerializer.Deserialize<PausaSettings>(File.ReadAllText(Path), JsonOptions) ?? new PausaSettings()
                : new PausaSettings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            settings = new PausaSettings();
        }
        settings.Normalize();
        return settings;
    }

    public void Save(PausaSettings settings)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var temp = Path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temp, Path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; never crash the app over them.
        }
    }

    /// <summary>Returns the persistent anonymous device ID, creating and saving one on first use.</summary>
    public string EnsureDeviceId(PausaSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DeviceId))
        {
            settings.DeviceId = Guid.NewGuid().ToString().ToUpperInvariant();
            Save(settings);
        }
        return settings.DeviceId!;
    }

    /// <summary>Forgets the install ID, so turning usage sharing back on starts a fresh, unlinked one.</summary>
    public void ResetDeviceId(PausaSettings settings)
    {
        settings.DeviceId = null;
        Save(settings);
    }
}
