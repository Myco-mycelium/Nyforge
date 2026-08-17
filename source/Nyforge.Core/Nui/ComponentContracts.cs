namespace Nyforge.Core.Nui;

/// <summary>
/// A single component's declared API contract: what properties it has,
/// what events it can raise, and (eventually) what Nyrqis actions it can
/// invoke.  Regenerated from the Nyrqis API Registry by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record ComponentContract(
    string Type,
    string Category,
    IReadOnlyList<string> Properties,
    IReadOnlyList<string> Events,
    IReadOnlyList<string>? Actions = null)
{
    /// <summary>Component-instance actions a behavior's DO clause can target on this type. Empty if none declared.</summary>
    public IReadOnlyList<string> Actions { get; init; } = Actions ?? Array.Empty<string>();
}

/// <summary>
/// The NUI component vocabulary — auto-generated from the Nyrqis API
/// Registry (engineering/registry/nui-api-v1.json).  Anything the
/// Component Palette shows in Nyforge.Shell must come from here
/// (NFC-001 §4.3).  To add or change a component, edit the registry
/// and re-run:  python tools/generate_contracts.py
/// </summary>
public static class ComponentContracts
{

    public static readonly IReadOnlyList<ComponentContract> All = new[]
    {
        // Basic
        new ComponentContract("Text", "Basic", new[] { "text", "visible" }, Array.Empty<string>()),
        new ComponentContract("Icon", "Basic", new[] { "glyph", "visible" }, Array.Empty<string>()),
        new ComponentContract("Image", "Basic", new[] { "source", "visible" }, Array.Empty<string>()),
        new ComponentContract("Button", "Basic", new[] { "text", "icon", "enabled", "visible" }, new[] { "clicked", "pressed", "released" }),
        new ComponentContract("Link", "Basic", new[] { "text", "target", "enabled" }, new[] { "clicked" }),
        new ComponentContract("Input", "Basic", new[] { "value", "placeholder", "enabled" }, new[] { "changed", "submitted" }),
        new ComponentContract("PasswordField", "Basic", new[] { "value", "placeholder", "enabled" }, new[] { "changed", "submitted" }),
        new ComponentContract("Checkbox", "Basic", new[] { "checked", "label", "enabled" }, new[] { "changed" }),
        new ComponentContract("Radio", "Basic", new[] { "selected", "label", "group", "enabled" }, new[] { "changed" }),
        new ComponentContract("Toggle", "Basic", new[] { "value", "label", "enabled" }, new[] { "changed" }),
        new ComponentContract("Slider", "Basic", new[] { "value", "min", "max", "enabled" }, new[] { "changed" }),
        new ComponentContract("ProgressBar", "Basic", new[] { "value", "min", "max" }, Array.Empty<string>()),
        // Layout
        new ComponentContract("Container", "Layout", new[] { "padding", "background" }, Array.Empty<string>()),
        new ComponentContract("Stack", "Layout", new[] { "orientation", "spacing" }, Array.Empty<string>()),
        new ComponentContract("Grid", "Layout", new[] { "columns", "rows", "spacing" }, Array.Empty<string>()),
        new ComponentContract("FlexLayout", "Layout", new[] { "direction", "wrap", "gap" }, Array.Empty<string>()),
        new ComponentContract("SplitView", "Layout", new[] { "orientation", "splitRatio" }, Array.Empty<string>()),
        new ComponentContract("ScrollView", "Layout", new[] { "direction" }, Array.Empty<string>()),
        new ComponentContract("Card", "Layout", new[] { "elevation", "padding" }, Array.Empty<string>()),
        new ComponentContract("Panel", "Layout", new[] { "background", "padding" }, Array.Empty<string>()),
        new ComponentContract("Toolbar", "Layout", new[] { "background" }, Array.Empty<string>()),
        new ComponentContract("StatusBar", "Layout", new[] { "background" }, Array.Empty<string>()),
        // System
        new ComponentContract("Window", "System", new[] { "title", "resizable", "width", "height" }, new[] { "opened", "closed" }, Actions: new[] { "Close" }),
        new ComponentContract("Dialog", "System", new[] { "title", "modal" }, new[] { "opened", "closed" }, Actions: new[] { "Open", "Close" }),
        new ComponentContract("Notification", "System", new[] { "title", "message", "severity" }, new[] { "dismissed" }, Actions: new[] { "Dismiss" }),
        // Navigation
        new ComponentContract("Sidebar", "Navigation", new[] { "width", "collapsible" }, Array.Empty<string>(), Actions: new[] { "Toggle" }),
        new ComponentContract("NavigationRail", "Navigation", new[] { "collapsible" }, Array.Empty<string>(), Actions: new[] { "Toggle" }),
        new ComponentContract("Tabs", "Navigation", new[] { "selectedIndex" }, new[] { "changed" }, Actions: new[] { "SetSelectedIndex" }),
        new ComponentContract("Breadcrumbs", "Navigation", new[] { "items" }, new[] { "itemClicked" }),
    };

    private static readonly Dictionary<string, ComponentContract> ByType =
        All.ToDictionary(c => c.Type, StringComparer.Ordinal);

    public static bool TryGet(string type, out ComponentContract? contract) =>
        ByType.TryGetValue(type, out contract);

    public static IEnumerable<ComponentContract> ByCategory(string category) =>
        All.Where(c => string.Equals(c.Category, category, StringComparison.Ordinal));
}
