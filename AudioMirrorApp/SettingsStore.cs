using System.Text.Json;

namespace AudioMirrorApp;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioMirror");

    private static string LegacySettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            var path = File.Exists(SettingsPath)
                ? SettingsPath
                : File.Exists(LegacySettingsPath)
                    ? LegacySettingsPath
                    : "";

            if (string.IsNullOrEmpty(path))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
            if (string.Equals(path, LegacySettingsPath, StringComparison.OrdinalIgnoreCase) && !File.Exists(SettingsPath))
            {
                Save(settings);
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
