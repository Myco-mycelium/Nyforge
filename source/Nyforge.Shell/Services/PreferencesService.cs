using System.Text.Json;

namespace Nyforge.Shell.Services;

/// <summary>
/// Persistent preferences for Nyforge — appearance, editor behavior,
/// canvas settings, and export options. Stored in the OS-standard
/// application data directory.
/// </summary>
public sealed class PreferencesService
{
    private sealed class PreferencesData
    {
        // Appearance
        public string? Theme { get; set; }
        public double CanvasWidth { get; set; } = 1024;
        public double CanvasHeight { get; set; } = 768;
        public bool AutoSave { get; set; } = true;

        // Editor
        public bool SnapToGrid { get; set; } = true;
        public int GridSize { get; set; } = 8;
        public bool ShowAlignmentGuides { get; set; } = true;
        public bool ShowSmartGuides { get; set; } = true;
        public int NudgeAmount { get; set; } = 4;
        public int ShiftMultiplier { get; set; } = 5;
        public bool ShowComponentLabels { get; set; } = true;
        public bool ShowComponentIds { get; set; } = true;
        public bool EnableMultiSelect { get; set; } = true;

        // Canvas
        public bool ShowGridOverlay { get; set; }
        public bool ShowRulers { get; set; }
        public bool ShowComponentBounds { get; set; } = true;
        public bool ShowSelectionHandles { get; set; } = true;
        public bool ZoomToFitOnSelection { get; set; }

        // Export
        public string ExportFormat { get; set; } = "nstudio";
        public bool ValidateBeforeExport { get; set; } = true;
        public bool RunValidatorInCiMode { get; set; }

        // Home screen
        public string? HomeScreenPath { get; set; }
    }

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Nyforge",
        "preferences.json");

    private PreferencesData _data;

    // --- Appearance ---
    public string Theme
    {
        get => _data.Theme ?? "Eclipse";
        set { _data.Theme = value; Save(); }
    }

    public double CanvasWidth
    {
        get => _data.CanvasWidth;
        set { _data.CanvasWidth = value; Save(); }
    }

    public double CanvasHeight
    {
        get => _data.CanvasHeight;
        set { _data.CanvasHeight = value; Save(); }
    }

    public bool AutoSave
    {
        get => _data.AutoSave;
        set { _data.AutoSave = value; Save(); }
    }

    // --- Editor ---
    public bool SnapToGrid
    {
        get => _data.SnapToGrid;
        set { _data.SnapToGrid = value; Save(); }
    }

    public int GridSize
    {
        get => _data.GridSize;
        set { _data.GridSize = value; Save(); }
    }

    public bool ShowAlignmentGuides
    {
        get => _data.ShowAlignmentGuides;
        set { _data.ShowAlignmentGuides = value; Save(); }
    }

    public bool ShowSmartGuides
    {
        get => _data.ShowSmartGuides;
        set { _data.ShowSmartGuides = value; Save(); }
    }

    public int NudgeAmount
    {
        get => _data.NudgeAmount;
        set { _data.NudgeAmount = value; Save(); }
    }

    public int ShiftMultiplier
    {
        get => _data.ShiftMultiplier;
        set { _data.ShiftMultiplier = value; Save(); }
    }

    public bool ShowComponentLabels
    {
        get => _data.ShowComponentLabels;
        set { _data.ShowComponentLabels = value; Save(); }
    }

    public bool ShowComponentIds
    {
        get => _data.ShowComponentIds;
        set { _data.ShowComponentIds = value; Save(); }
    }

    public bool EnableMultiSelect
    {
        get => _data.EnableMultiSelect;
        set { _data.EnableMultiSelect = value; Save(); }
    }

    // --- Canvas ---
    public bool ShowGridOverlay
    {
        get => _data.ShowGridOverlay;
        set { _data.ShowGridOverlay = value; Save(); }
    }

    public bool ShowRulers
    {
        get => _data.ShowRulers;
        set { _data.ShowRulers = value; Save(); }
    }

    public bool ShowComponentBounds
    {
        get => _data.ShowComponentBounds;
        set { _data.ShowComponentBounds = value; Save(); }
    }

    public bool ShowSelectionHandles
    {
        get => _data.ShowSelectionHandles;
        set { _data.ShowSelectionHandles = value; Save(); }
    }

    // --- Export ---
    public string ExportFormat
    {
        get => _data.ExportFormat;
        set { _data.ExportFormat = value; Save(); }
    }

    public bool ValidateBeforeExport
    {
        get => _data.ValidateBeforeExport;
        set { _data.ValidateBeforeExport = value; Save(); }
    }

    // --- Home screen ---
    public string? HomeScreenPath
    {
        get => _data.HomeScreenPath;
        set { _data.HomeScreenPath = value; Save(); }
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
            return new PreferencesData();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (dir is not null) Directory.CreateDirectory(dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Preferences are a convenience, not critical path.
        }
    }
}
