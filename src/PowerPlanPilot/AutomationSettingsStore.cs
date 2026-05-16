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

    public AutomationSettingsLoadResult Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AutomationSettingsLoadResult(new AutomationSettings(), null);
            }

            using var stream = File.OpenRead(_settingsPath);
            var settings = JsonSerializer.Deserialize<AutomationSettings>(stream, SerializerOptions)
                ?? new AutomationSettings();
            settings.Normalize();
            return new AutomationSettingsLoadResult(settings, null);
        }
        catch (Exception ex)
        {
            return new AutomationSettingsLoadResult(
                new AutomationSettings(),
                $"Settings reset: {ex.Message}");
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
