using System.Globalization;
using Avalonia.Data.Converters;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Which of the small set of types the v0.3 preview renders interactively.
/// Anything not covered here falls through to the placeholder template in
/// PreviewWindow.axaml — see the honesty note there (NFM-000 §2.1).
/// </summary>
public static class PreviewTypeConverters
{
    private static readonly HashSet<string> ButtonTypes = new(StringComparer.Ordinal) { "Button", "Link" };
    private static readonly HashSet<string> ToggleTypes = new(StringComparer.Ordinal) { "Toggle", "Checkbox" };
    private static readonly HashSet<string> TextTypes = new(StringComparer.Ordinal) { "Text" };

    public static readonly IValueConverter IsButton = new FuncValueConverter<string?, bool>(t => t is not null && ButtonTypes.Contains(t));
    public static readonly IValueConverter IsToggleLike = new FuncValueConverter<string?, bool>(t => t is not null && ToggleTypes.Contains(t));
    public static readonly IValueConverter IsText = new FuncValueConverter<string?, bool>(t => t is not null && TextTypes.Contains(t));
    public static readonly IValueConverter IsPlaceholder = new FuncValueConverter<string?, bool>(
        t => t is not null && !ButtonTypes.Contains(t) && !ToggleTypes.Contains(t) && !TextTypes.Contains(t));
}
