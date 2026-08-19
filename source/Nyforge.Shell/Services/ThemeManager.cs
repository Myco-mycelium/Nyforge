using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Nyforge.Shell.Services;

/// <summary>
/// Swaps which theme token dictionary (Themes/*.axaml) is merged into
/// Application. Persists the selected theme across restarts via PreferencesService.
/// </summary>
public sealed class ThemeManager
{
    public static readonly IReadOnlyDictionary<string, string> AvailableThemes =
        new Dictionary<string, string>
        {
            ["Eclipse"] = "avares://Nyforge/Themes/Eclipse.axaml",
            ["Solar"] = "avares://Nyforge/Themes/Solar.axaml",
        };

    private readonly Application _app;
    private readonly PreferencesService _preferences;
    private StyleInclude? _currentThemeStyle;

    public string CurrentTheme { get; private set; } = "Eclipse";

    public event EventHandler<string>? ThemeChanged;

    public ThemeManager(Application app, PreferencesService preferences)
    {
        _app = app;
        _preferences = preferences;

        // Restore saved theme, falling back to Eclipse
        var savedTheme = _preferences.Theme;
        if (!AvailableThemes.ContainsKey(savedTheme)) savedTheme = "Eclipse";
        CurrentTheme = savedTheme;

        // App.axaml loads Eclipse at startup — track that reference
        _currentThemeStyle = _app.Styles
            .OfType<StyleInclude>()
            .FirstOrDefault(s => s.Source?.OriginalString.Contains("/Themes/") == true);

        // If saved theme differs from default, apply it
        if (savedTheme != "Eclipse")
        {
            ApplyTheme(savedTheme);
        }
    }

    public void SetTheme(string themeName)
    {
        if (!AvailableThemes.TryGetValue(themeName, out var uri))
        {
            throw new ArgumentException(
                $"Unknown theme '{themeName}'. Registered themes: {string.Join(", ", AvailableThemes.Keys)}");
        }

        ApplyTheme(themeName);
        _preferences.Theme = themeName;
        ThemeChanged?.Invoke(this, themeName);
    }

    private void ApplyTheme(string themeName)
    {
        if (!AvailableThemes.TryGetValue(themeName, out var uri)) return;

        if (_currentThemeStyle is not null)
        {
            _app.Styles.Remove(_currentThemeStyle);
        }

        var newStyle = new StyleInclude(new Uri("avares://Nyforge/"))
        {
            Source = new Uri(uri)
        };
        _app.Styles.Add(newStyle);
        _currentThemeStyle = newStyle;
        CurrentTheme = themeName;
    }
}
