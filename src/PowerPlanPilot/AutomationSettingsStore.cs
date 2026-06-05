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
        var settingsDirectory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(settingsDirectory);

        var tempPath = Path.Combine(
            settingsDirectory,
            $"{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_settingsPath))
            {
                File.Replace(tempPath, _settingsPath, null);
            }
            else
            {
                File.Move(tempPath, _settingsPath);
            }
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
