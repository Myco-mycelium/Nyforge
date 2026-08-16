using System.Text.Json;

namespace Nyforge.Shell.Services;

/// <summary>
/// Tiny local preferences file — currently just "which .nstudio file
/// should Forge's own Home screen render." This is what makes
/// "redesign the Home screen, then update whenever I want" durable across
/// restarts rather than a one-off in-session swap. Not a general settings
/// system; add fields deliberately, not speculatively.
/// </summary>
public sealed class PreferencesService
{
    private sealed class PreferencesData
    {
        public string? HomeScreenPath { get; set; }
    }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Nyforge",
        "preferences.json");

    private PreferencesData _data;

    public string? HomeScreenPath
    {
        get => _data.HomeScreenPath;
        set
        {
            _data.HomeScreenPath = value;
            Save();
        }
    }

    public PreferencesService()
    {
        _data = Load();
    }

    private static PreferencesData Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new PreferencesData();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<PreferencesData>(json) ?? new PreferencesData();
        }
        catch
        {
            // A corrupt or unreadable prefs file should never block startup —
            // fall back to defaults rather than crash the editor over it.
            return new PreferencesData();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Preferences are a convenience, not a critical path — a failed
            // write shouldn't surface as an error to the person using Forge.
        }
    }
}
