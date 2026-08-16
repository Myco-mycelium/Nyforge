using Avalonia;
using Avalonia.Markup.Xaml.Styling;

namespace Nyforge.Shell.Services;

/// <summary>
/// Swaps which theme token dictionary (Themes/*.axaml) is merged into
/// Application.Styles. See docs/how-to/switching-themes.md and NFC-001 §6.
/// </summary>
public sealed class ThemeManager
{
    /// <summary>
    /// Registered themes, name -> avares URI. Add an entry here (and the
    /// matching .axaml file) to introduce a new theme without touching
    /// component code, per NFC-001 §6.2.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AvailableThemes =
        new Dictionary<string, string>
        {
            // avares URIs must match the assembly's simple name (csproj
            // <AssemblyName>Nyforge</AssemblyName>), not the project name.
            ["Eclipse"] = "avares://Nyforge/Themes/Eclipse.axaml",
            ["Solar"] = "avares://Nyforge/Themes/Solar.axaml",
        };

    private readonly Application _app;
    private StyleInclude? _currentThemeStyle;

    public string CurrentTheme { get; private set; } = "Eclipse";

    public event EventHandler<string>? ThemeChanged;

    public ThemeManager(Application app)
    {
        _app = app;
        // App.axaml already loads Eclipse at startup; track that reference
        // so subsequent switches replace rather than stack dictionaries.
        _currentThemeStyle = _app.Styles
            .OfType<StyleInclude>()
            .FirstOrDefault(s => s.Source?.OriginalString.Contains("/Themes/") == true);
    }

    public void SetTheme(string themeName)
    {
        if (!AvailableThemes.TryGetValue(themeName, out var uri))
        {
            throw new ArgumentException($"Unknown theme '{themeName}'. Registered themes: {string.Join(", ", AvailableThemes.Keys)}");
        }

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
        ThemeChanged?.Invoke(this, themeName);
    }
}
