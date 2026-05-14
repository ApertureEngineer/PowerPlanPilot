using System.Text.Json;

namespace PowerPlanPilot;

internal sealed class AutomationSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    public AutomationSettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "PowerPlanPilot", "automation.json");
    }

    public AutomationSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AutomationSettings();
            }

            using var stream = File.OpenRead(_settingsPath);
            var settings = JsonSerializer.Deserialize<AutomationSettings>(stream, SerializerOptions)
                ?? new AutomationSettings();
            settings.Normalize();
            return settings;
        }
        catch
        {
            return new AutomationSettings();
        }
    }

    public void Save(AutomationSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

        using var stream = File.Create(_settingsPath);
        JsonSerializer.Serialize(stream, settings, SerializerOptions);
    }
}
