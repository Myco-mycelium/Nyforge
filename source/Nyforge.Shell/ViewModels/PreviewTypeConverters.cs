using System.Globalization;
using Avalonia.Data.Converters;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Type converters for the Preview window's DataTemplate selection.
/// Now delegates to the <see cref="ComponentRendererRegistry"/> so the
/// set of interactive types is driven by registered renderers, not
/// hardcoded strings. (NFM-000 §2.1 — the canvas is truthful.)
/// </summary>
public static class PreviewTypeConverters
{
    private static ComponentRendererRegistry? _registry;

    /// <summary>
    /// Initialise with the active renderer registry. Call once from
    /// PreviewViewModel constructor before the first render.
    /// </summary>
    public static void Initialise(ComponentRendererRegistry registry) => _registry = registry;

    private static bool HasType(string? type) =>
        type is not null && _registry is not null && _registry.HasRenderer(type);

    private static bool IsTypeIn(string? type, params string[] types) =>
        type is not null && Array.Exists(types, t => string.Equals(t, type, StringComparison.Ordinal));

    // ---- interactive leaf converters ----
    public static readonly IValueConverter IsButton = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "Button"));
    public static readonly IValueConverter IsToggleLike = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "Toggle", "Switch", "Checkbox", "Radio"));
    public static readonly IValueConverter IsText = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "Text", "Label", "Heading", "Paragraph"));
    public static readonly IValueConverter IsSlider = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "Slider"));
    public static readonly IValueConverter IsProgressBar = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "ProgressBar"));
    public static readonly IValueConverter IsImage = new FuncValueConverter<string?, bool>(
        t => IsTypeIn(t, "Image"));

    /// <summary>
    /// True when the type has a registered renderer but is NOT one of
    /// the explicitly handled interactive types above — i.e. it
    /// renders as the generic labelled placeholder.
    /// </summary>
    public static readonly IValueConverter IsPlaceholder = new FuncValueConverter<string?, bool>(
        t => HasType(t)
             && !IsTypeIn(t, "Button", "Link",
                           "Toggle", "Switch", "Checkbox", "Radio",
                           "Text", "Label", "Heading", "Paragraph",
                           "Slider", "ProgressBar", "Image"));
}
