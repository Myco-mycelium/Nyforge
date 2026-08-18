using Nyforge.Core.Nui;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Renderers;

// ---- Text / Label / Heading / Paragraph ----
public sealed class TextRenderer : IPropertyRenderer, IEventRenderer
{
    public string Name => "Text";
    public IReadOnlyList<string> SupportedTypes => new[] { "Text", "Label", "Heading", "Paragraph" };
    public IReadOnlyList<string> SupportedEvents => Array.Empty<string>();

    public IReadOnlyDictionary<string, object?> DefaultProperties(NuiComponent c) =>
        new Dictionary<string, object?>
        {
            ["text"] = c.Properties.TryGetValue("text", out var t) ? t : c.Type,
            ["fontSize"] = c.Type == "Heading" ? 24.0 : c.Type == "Paragraph" ? 16.0 : 14.0,
        };
}

// ---- Button ----
public sealed class ButtonRenderer : IEventRenderer
{
    public string Name => "Button";
    public IReadOnlyList<string> SupportedTypes => new[] { "Button" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked", "pressed", "released" };
}

// ---- Link ----
public sealed class LinkRenderer : IEventRenderer
{
    public string Name => "Link";
    public IReadOnlyList<string> SupportedTypes => new[] { "Link" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked" };
}

// ---- Checkbox ----
public sealed class CheckboxRenderer : IEventRenderer
{
    public string Name => "Checkbox";
    public IReadOnlyList<string> SupportedTypes => new[] { "Checkbox" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- Toggle / Switch ----
public sealed class ToggleRenderer : IEventRenderer
{
    public string Name => "Toggle";
    public IReadOnlyList<string> SupportedTypes => new[] { "Toggle", "Switch" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- Slider ----
public sealed class SliderRenderer : IEventRenderer
{
    public string Name => "Slider";
    public IReadOnlyList<string> SupportedTypes => new[] { "Slider" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- ProgressBar ----
public sealed class ProgressBarRenderer : IComponentRenderer
{
    public string Name => "ProgressBar";
    public IReadOnlyList<string> SupportedTypes => new[] { "ProgressBar" };
}

// ---- Radio ----
public sealed class RadioRenderer : IEventRenderer
{
    public string Name => "Radio";
    public IReadOnlyList<string> SupportedTypes => new[] { "Radio" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- Input ----
public sealed class InputRenderer : IEventRenderer
{
    public string Name => "Input";
    public IReadOnlyList<string> SupportedTypes => new[] { "Input" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed", "submitted" };
}

// ---- PasswordField ----
public sealed class PasswordFieldRenderer : IEventRenderer
{
    public string Name => "PasswordField";
    public IReadOnlyList<string> SupportedTypes => new[] { "PasswordField" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed", "submitted" };
}

// ---- Icon ----
public sealed class IconRenderer : IComponentRenderer
{
    public string Name => "Icon";
    public IReadOnlyList<string> SupportedTypes => new[] { "Icon" };
}

// ---- Image ----
public sealed class ImageRenderer : IComponentRenderer
{
    public string Name => "Image";
    public IReadOnlyList<string> SupportedTypes => new[] { "Image" };
}

// ---- DatePicker ----
public sealed class DatePickerRenderer : IEventRenderer
{
    public string Name => "DatePicker";
    public IReadOnlyList<string> SupportedTypes => new[] { "DatePicker" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- TimePicker ----
public sealed class TimePickerRenderer : IEventRenderer
{
    public string Name => "TimePicker";
    public IReadOnlyList<string> SupportedTypes => new[] { "TimePicker" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed" };
}

// ---- Search ----
public sealed class SearchRenderer : IEventRenderer
{
    public string Name => "Search";
    public IReadOnlyList<string> SupportedTypes => new[] { "Search" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed", "submitted" };
}

// ---- AppGrid ----
public sealed class AppGridRenderer : IComponentRenderer
{
    public string Name => "AppGrid";
    public IReadOnlyList<string> SupportedTypes => new[] { "AppGrid" };
}

// ---- Notification ----
public sealed class NotificationRenderer : IComponentRenderer
{
    public string Name => "Notification";
    public IReadOnlyList<string> SupportedTypes => new[] { "Notification" };
}

// ---- Application ----
public sealed class ApplicationRenderer : IComponentRenderer
{
    public string Name => "Application";
    public IReadOnlyList<string> SupportedTypes => new[] { "Application" };
}
