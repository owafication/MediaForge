using System.Text.Json;
using System.IO;
using MediaForge.Models;
using MediaForge.Services.Runtime;

namespace MediaForge.Services;

public sealed class SettingsService
{
    private readonly string? _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService()
    {
        try
        {
            var directory = ApplicationDataPaths.GetRoamingProductPath();
            Directory.CreateDirectory(directory);
            _settingsPath = Path.Combine(directory, "settings.json");
        }
        catch
        {
            _settingsPath = null;
        }
    }

    public AppSettings Load()
    {
        try
        {
            if (_settingsPath is null) return new AppSettings();
            if (!File.Exists(_settingsPath)) return new AppSettings();
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var settingsPath = _settingsPath ??
            throw new InvalidOperationException("The MediaForge settings folder is unavailable.");

        var tempPath = settingsPath + ".tmp";
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, settingsPath, true);
    }
}
