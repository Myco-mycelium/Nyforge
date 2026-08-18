namespace Nyforge.Core.Nui;

/// <summary>
/// Typed metadata for one component property (NFS-006 / the Nyrqis API
/// Registry): what the Inspector renders, validates against, and binds.
/// Auto-generated from engineering/registry/nui-api-v1.json by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record PropertyDefinition(
    string Name,
    string Type,
    object? DefaultValue = null,
    bool Bindable = true,
    bool Required = false,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? EnumValues = null,
    string? Units = null)
{
    /// <summary>Enum choices when <see cref="Type"/> is "enum"; empty otherwise.</summary>
    public IReadOnlyList<string> EnumValues { get; init; } = EnumValues ?? Array.Empty<string>();
}

/// <summary>
/// Per-component property metadata — the typed contract the Inspector
/// builds its editors from (one editor per property, chosen by
/// <see cref="PropertyDefinition.Type"/>). Generated from the Nyrqis
/// API Registry; add or change a property in the registry and re-run
/// tools/generate_contracts.py.
/// </summary>
public static class PropertyDefinitions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<PropertyDefinition>> ByType =
        new Dictionary<string, IReadOnlyList<PropertyDefinition>>
        {

            ["Text"] = new[]
            {
                new PropertyDefinition("text", "string", DefaultValue: ""),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
            },
            ["Icon"] = new[]
            {
                new PropertyDefinition("glyph", "string", DefaultValue: ""),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
            },
            ["Image"] = new[]
            {
                new PropertyDefinition("source", "string", DefaultValue: ""),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
            },
            ["Button"] = new[]
            {
                new PropertyDefinition("text", "string", DefaultValue: ""),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
            },
            ["Link"] = new[]
            {
                new PropertyDefinition("text", "string", DefaultValue: ""),
                new PropertyDefinition("target", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["Input"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("placeholder", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["PasswordField"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("placeholder", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["Checkbox"] = new[]
            {
                new PropertyDefinition("checked", "boolean", DefaultValue: false),
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["Radio"] = new[]
            {
                new PropertyDefinition("selected", "boolean", DefaultValue: false),
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("group", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["Toggle"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["Slider"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0, Min: 0, Max: 100),
                new PropertyDefinition("min", "number", DefaultValue: 0, Min: 0, Max: 1000000),
                new PropertyDefinition("max", "number", DefaultValue: 100, Min: 0, Max: 1000000),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["ProgressBar"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0, Min: 0, Max: 100),
                new PropertyDefinition("min", "number", DefaultValue: 0),
                new PropertyDefinition("max", "number", DefaultValue: 100),
            },
            ["Container"] = new[]
            {
                new PropertyDefinition("padding", "number", DefaultValue: 0),
                new PropertyDefinition("background", "string", DefaultValue: ""),
            },
            ["Stack"] = new[]
            {
                new PropertyDefinition("orientation", "enum", DefaultValue: "vertical", EnumValues: new[] { "vertical", "horizontal" }),
                new PropertyDefinition("spacing", "number", DefaultValue: 0),
            },
            ["Grid"] = new[]
            {
                new PropertyDefinition("columns", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("rows", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("spacing", "number", DefaultValue: 0),
            },
            ["FlexLayout"] = new[]
            {
                new PropertyDefinition("direction", "enum", DefaultValue: "row", EnumValues: new[] { "row", "column" }),
                new PropertyDefinition("wrap", "boolean", DefaultValue: false),
                new PropertyDefinition("gap", "number", DefaultValue: 0),
            },
            ["SplitView"] = new[]
            {
                new PropertyDefinition("orientation", "enum", DefaultValue: "vertical", EnumValues: new[] { "vertical", "horizontal" }),
                new PropertyDefinition("splitRatio", "number", DefaultValue: 0.5),
            },
            ["ScrollView"] = new[]
            {
                new PropertyDefinition("direction", "enum", DefaultValue: "row", EnumValues: new[] { "row", "column" }),
            },
            ["Card"] = new[]
            {
                new PropertyDefinition("elevation", "number", DefaultValue: 0, Units: "dp"),
                new PropertyDefinition("padding", "number", DefaultValue: 0),
            },
            ["Panel"] = new[]
            {
                new PropertyDefinition("background", "string", DefaultValue: ""),
                new PropertyDefinition("padding", "number", DefaultValue: 0),
            },
            ["Toolbar"] = new[]
            {
                new PropertyDefinition("background", "string", DefaultValue: ""),
            },
            ["StatusBar"] = new[]
            {
                new PropertyDefinition("background", "string", DefaultValue: ""),
            },
            ["Window"] = new[]
            {
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("resizable", "boolean", DefaultValue: true),
                new PropertyDefinition("width", "number", DefaultValue: 0, Units: "px"),
                new PropertyDefinition("height", "number", DefaultValue: 0, Units: "px"),
            },
            ["Dialog"] = new[]
            {
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("modal", "boolean", DefaultValue: false),
            },
            ["Notification"] = new[]
            {
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("message", "string", DefaultValue: ""),
                new PropertyDefinition("severity", "enum", DefaultValue: "info", EnumValues: new[] { "info", "warning", "error" }),
            },
            ["Sidebar"] = new[]
            {
                new PropertyDefinition("width", "number", DefaultValue: 0, Units: "px"),
                new PropertyDefinition("collapsible", "boolean", DefaultValue: true),
            },
            ["NavigationRail"] = new[]
            {
                new PropertyDefinition("collapsible", "boolean", DefaultValue: true),
            },
            ["Tabs"] = new[]
            {
                new PropertyDefinition("selectedIndex", "number", DefaultValue: -1),
            },
            ["Breadcrumbs"] = new[]
            {
                new PropertyDefinition("items", "array", DefaultValue: new object[] {  }),
            },
            ["DesktopSurface"] = new[]
            {
                new PropertyDefinition("wallpaper", "string", DefaultValue: ""),
                new PropertyDefinition("accent", "string", DefaultValue: ""),
                new PropertyDefinition("iconSize", "number", DefaultValue: 96, Units: "px"),
            },
            ["DesktopIcon"] = new[]
            {
                new PropertyDefinition("glyph", "string", DefaultValue: ""),
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("target", "string", DefaultValue: ""),
            },
            ["Taskbar"] = new[]
            {
                new PropertyDefinition("position", "enum", DefaultValue: "top", EnumValues: new[] { "top", "bottom", "left", "right" }),
                new PropertyDefinition("alignment", "enum", DefaultValue: "start", EnumValues: new[] { "start", "center", "end" }),
                new PropertyDefinition("autoHide", "boolean", DefaultValue: false),
                new PropertyDefinition("pinnedApps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("runningApps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("showClock", "boolean", DefaultValue: true),
                new PropertyDefinition("showTray", "boolean", DefaultValue: true),
            },
            ["StartMenu"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("pinnedApps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("recommendedApps", "array", DefaultValue: new object[] {  }),
            },
            ["SystemTray"] = new[]
            {
                new PropertyDefinition("icons", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
                new PropertyDefinition("badge", "number", DefaultValue: 0),
            },
            ["NotificationCenter"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("notifications", "array", DefaultValue: new object[] {  }),
            },
            ["QuickSettings"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("toggles", "array", DefaultValue: new object[] {  }),
            },
            ["WorkspaceSwitcher"] = new[]
            {
                new PropertyDefinition("workspaces", "number", DefaultValue: 3),
                new PropertyDefinition("currentWorkspace", "number", DefaultValue: 1),
            },
            ["WindowFrame"] = new[]
            {
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("resizable", "boolean", DefaultValue: true),
                new PropertyDefinition("movable", "boolean", DefaultValue: true),
                new PropertyDefinition("minimized", "boolean", DefaultValue: false),
                new PropertyDefinition("maximized", "boolean", DefaultValue: false),
            },
            ["WindowControls"] = new[]
            {
                new PropertyDefinition("showMinimize", "boolean", DefaultValue: true),
                new PropertyDefinition("showMaximize", "boolean", DefaultValue: true),
                new PropertyDefinition("showClose", "boolean", DefaultValue: true),
            },
            ["ContextMenu"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("items", "array", DefaultValue: new object[] {  }),
            },
            ["CommandPalette"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("query", "string", DefaultValue: ""),
                new PropertyDefinition("results", "array", DefaultValue: new object[] {  }),
            },
            ["Launcher"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("query", "string", DefaultValue: ""),
            },
            ["Search"] = new[]
            {
                new PropertyDefinition("query", "string", DefaultValue: ""),
                new PropertyDefinition("placeholder", "string", DefaultValue: ""),
                new PropertyDefinition("results", "array", DefaultValue: new object[] {  }),
            },
            ["PowerMenu"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
            },
            ["LockScreen"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("clockFormat", "enum", DefaultValue: "12h", EnumValues: new[] { "12h", "24h" }),
                new PropertyDefinition("wallpaper", "string", DefaultValue: ""),
            },
            ["Application"] = new[]
            {
                new PropertyDefinition("appId", "string", DefaultValue: ""),
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
                new PropertyDefinition("running", "boolean", DefaultValue: false),
                new PropertyDefinition("focused", "boolean", DefaultValue: false),
            },
            ["WidgetHost"] = new[]
            {
                new PropertyDefinition("widgets", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("layout", "enum", DefaultValue: "grid", EnumValues: new[] { "grid", "list" }),
                new PropertyDefinition("visible", "boolean", DefaultValue: true),
            },
            ["OSD"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("message", "string", DefaultValue: ""),
                new PropertyDefinition("timeout", "number", DefaultValue: 2),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
            },
            ["Login"] = new[]
            {
                new PropertyDefinition("open", "boolean", DefaultValue: false),
                new PropertyDefinition("username", "string", DefaultValue: ""),
                new PropertyDefinition("avatar", "string", DefaultValue: ""),
                new PropertyDefinition("hint", "string", DefaultValue: ""),
            },
            ["AppGrid"] = new[]
            {
                new PropertyDefinition("apps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("columns", "number", DefaultValue: 4, Min: 1),
                new PropertyDefinition("iconSize", "number", DefaultValue: 48, Units: "px"),
            },
            ["Clock"] = new[]
            {
                new PropertyDefinition("format", "enum", DefaultValue: "24h", EnumValues: new[] { "12h", "24h" }),
                new PropertyDefinition("showSeconds", "boolean", DefaultValue: false),
            },
            ["Dock"] = new[]
            {
                new PropertyDefinition("position", "enum", DefaultValue: "bottom", EnumValues: new[] { "top", "bottom", "left", "right" }),
                new PropertyDefinition("pinnedApps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("runningApps", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("autoHide", "boolean", DefaultValue: false),
                new PropertyDefinition("iconSize", "number", DefaultValue: 48, Units: "px"),
                new PropertyDefinition("magnify", "boolean", DefaultValue: true),
            },
            ["TitleBar"] = new[]
            {
                new PropertyDefinition("title", "string", DefaultValue: ""),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
            },
            ["List"] = new[]
            {
                new PropertyDefinition("items", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("selectedIndex", "number", DefaultValue: -1),
                new PropertyDefinition("multiple", "boolean", DefaultValue: false),
            },
            ["ListItem"] = new[]
            {
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
                new PropertyDefinition("selected", "boolean", DefaultValue: false),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
            },
            ["DataTable"] = new[]
            {
                new PropertyDefinition("columns", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("rows", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("selectedRow", "string", DefaultValue: ""),
            },
            ["TreeView"] = new[]
            {
                new PropertyDefinition("nodes", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("selectedNode", "string", DefaultValue: ""),
                new PropertyDefinition("expandedNodes", "array", DefaultValue: new object[] {  }),
            },
            ["Menu"] = new[]
            {
                new PropertyDefinition("items", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("open", "boolean", DefaultValue: false),
            },
            ["MenuItem"] = new[]
            {
                new PropertyDefinition("label", "string", DefaultValue: ""),
                new PropertyDefinition("icon", "string", DefaultValue: ""),
                new PropertyDefinition("shortcut", "string", DefaultValue: ""),
                new PropertyDefinition("enabled", "boolean", DefaultValue: true),
                new PropertyDefinition("checked", "boolean", DefaultValue: false),
            },
            ["Form"] = new[]
            {
                new PropertyDefinition("fields", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("submitLabel", "string", DefaultValue: ""),
                new PropertyDefinition("valid", "boolean", DefaultValue: true),
            },
            ["DatePicker"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("min", "number", DefaultValue: 0),
                new PropertyDefinition("max", "number", DefaultValue: 100),
                new PropertyDefinition("format", "string", DefaultValue: ""),
            },
            ["TimePicker"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("format", "string", DefaultValue: ""),
            },
            ["FilePicker"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("filter", "string", DefaultValue: ""),
                new PropertyDefinition("multiple", "boolean", DefaultValue: false),
                new PropertyDefinition("folder", "string", DefaultValue: ""),
            },
            ["SettingsPanel"] = new[]
            {
                new PropertyDefinition("sections", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("selectedSection", "string", DefaultValue: ""),
                new PropertyDefinition("searchable", "boolean", DefaultValue: true),
            },
            ["Video"] = new[]
            {
                new PropertyDefinition("source", "string", DefaultValue: ""),
                new PropertyDefinition("autoplay", "boolean", DefaultValue: false),
                new PropertyDefinition("loop", "boolean", DefaultValue: false),
                new PropertyDefinition("muted", "boolean", DefaultValue: false),
                new PropertyDefinition("volume", "number", DefaultValue: 50, Min: 0, Max: 100),
            },
            ["Audio"] = new[]
            {
                new PropertyDefinition("source", "string", DefaultValue: ""),
                new PropertyDefinition("autoplay", "boolean", DefaultValue: false),
                new PropertyDefinition("loop", "boolean", DefaultValue: false),
                new PropertyDefinition("volume", "number", DefaultValue: 50, Min: 0, Max: 100),
            },
            ["MediaPlayer"] = new[]
            {
                new PropertyDefinition("source", "string", DefaultValue: ""),
                new PropertyDefinition("playing", "boolean", DefaultValue: false),
                new PropertyDefinition("volume", "number", DefaultValue: 50, Min: 0, Max: 100),
                new PropertyDefinition("position", "number", DefaultValue: 0, Min: 0),
                new PropertyDefinition("muted", "boolean", DefaultValue: false),
            },
            ["Terminal"] = new[]
            {
                new PropertyDefinition("command", "string", DefaultValue: ""),
                new PropertyDefinition("cwd", "string", DefaultValue: ""),
                new PropertyDefinition("history", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("prompt", "string", DefaultValue: ""),
            },
            ["CodeEditor"] = new[]
            {
                new PropertyDefinition("value", "number", DefaultValue: 0),
                new PropertyDefinition("language", "string", DefaultValue: ""),
                new PropertyDefinition("readOnly", "boolean", DefaultValue: false),
                new PropertyDefinition("theme", "string", DefaultValue: ""),
                new PropertyDefinition("lineNumbers", "boolean", DefaultValue: true),
            },
            ["LogViewer"] = new[]
            {
                new PropertyDefinition("entries", "array", DefaultValue: new object[] {  }),
                new PropertyDefinition("filter", "string", DefaultValue: ""),
                new PropertyDefinition("level", "enum", DefaultValue: "debug", EnumValues: new[] { "debug", "info", "warning", "error" }),
                new PropertyDefinition("follow", "boolean", DefaultValue: true),
            },
        };

    /// <summary>Metadata for every property of the given component type; empty if unknown.</summary>
    public static IReadOnlyList<PropertyDefinition> For(string type) =>
        ByType.TryGetValue(type, out var defs) ? defs : Array.Empty<PropertyDefinition>();
}
