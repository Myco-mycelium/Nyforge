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
        new ComponentContract("SearchBox", "Basic", new[] { "placeholder", "query", "visible", "mode" }, new[] { "changed", "submitted" }, Actions: new[] { "Open", "Close", "Clear" }),
        new ComponentContract("Button", "Basic", new[] { "text", "icon", "enabled", "visible", "tooltip" }, new[] { "clicked", "pressed", "released" }),
        new ComponentContract("Link", "Basic", new[] { "text", "target", "enabled" }, new[] { "clicked" }),
        new ComponentContract("Input", "Basic", new[] { "value", "placeholder", "enabled" }, new[] { "changed", "submitted" }),
        new ComponentContract("PasswordField", "Basic", new[] { "value", "placeholder", "enabled" }, new[] { "changed", "submitted" }),
        new ComponentContract("Checkbox", "Basic", new[] { "checked", "label", "enabled" }, new[] { "changed" }),
        new ComponentContract("Radio", "Basic", new[] { "selected", "label", "group", "enabled" }, new[] { "changed" }),
        new ComponentContract("Toggle", "Basic", new[] { "value", "label", "enabled" }, new[] { "changed" }),
        new ComponentContract("Slider", "Basic", new[] { "value", "min", "max", "enabled" }, new[] { "changed" }),
        new ComponentContract("ProgressBar", "Basic", new[] { "value", "min", "max" }, Array.Empty<string>()),
        // Layout
        new ComponentContract("Container", "Layout", new[] { "padding", "background", "title" }, Array.Empty<string>()),
        new ComponentContract("Stack", "Layout", new[] { "orientation", "direction", "spacing" }, Array.Empty<string>()),
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
        // Shell
        new ComponentContract("DesktopSurface", "Shell", new[] { "wallpaper", "accent", "iconSize" }, Array.Empty<string>()),
        new ComponentContract("DesktopIcon", "Shell", new[] { "glyph", "label", "target" }, new[] { "activated" }, Actions: new[] { "Launch" }),
        new ComponentContract("Taskbar", "Shell", new[] { "position", "alignment", "autoHide", "pinnedApps", "runningApps", "showClock", "showTray", "showStartButton", "showSearch", "showTaskView" }, Array.Empty<string>()),
        new ComponentContract("StartMenu", "Shell", new[] { "open", "pinnedApps", "recommendedApps", "visible", "searchPlaceholder", "showCategories", "showPinned", "showRecent" }, new[] { "opened", "closed" }, Actions: new[] { "Open", "Close", "Toggle" }),
        new ComponentContract("SystemTray", "Shell", new[] { "icons", "visible", "badge", "showClock", "showVolume", "showNetwork" }, Array.Empty<string>()),
        new ComponentContract("NotificationCenter", "Shell", new[] { "open", "notifications", "visible", "showQuickSettings", "showNotifications", "showCalendar" }, new[] { "opened", "closed", "notificationClicked" }, Actions: new[] { "Open", "Close", "Clear" }),
        new ComponentContract("QuickSettings", "Shell", new[] { "open", "toggles", "showWifi", "showBluetooth", "showBrightness", "showVolume", "showDoNotDisturb" }, new[] { "opened", "closed", "toggleChanged" }, Actions: new[] { "Open", "Close", "Toggle" }),
        new ComponentContract("WorkspaceSwitcher", "Shell", new[] { "workspaces", "currentWorkspace" }, new[] { "changed" }, Actions: new[] { "SetCurrentWorkspace" }),
        new ComponentContract("WindowFrame", "Shell", new[] { "title", "resizable", "movable", "minimized", "maximized" }, new[] { "opened", "closed", "moved", "resized" }, Actions: new[] { "Minimize", "Maximize", "Restore", "Close" }),
        new ComponentContract("WindowControls", "Shell", new[] { "showMinimize", "showMaximize", "showClose" }, new[] { "minimizeClicked", "maximizeClicked", "closeClicked" }),
        new ComponentContract("ContextMenu", "Shell", new[] { "open", "items" }, new[] { "opened", "closed", "itemClicked" }, Actions: new[] { "Open", "Close" }),
        new ComponentContract("CommandPalette", "Shell", new[] { "open", "query", "results" }, new[] { "opened", "closed", "queryChanged", "commandExecuted" }, Actions: new[] { "Open", "Close", "SetQuery" }),
        new ComponentContract("Launcher", "Shell", new[] { "open", "query" }, new[] { "opened", "closed", "appLaunched" }, Actions: new[] { "Open", "Close", "Launch" }),
        new ComponentContract("Search", "Shell", new[] { "query", "placeholder", "results" }, new[] { "changed", "submitted" }, Actions: new[] { "Clear", "Submit" }),
        new ComponentContract("PowerMenu", "Shell", new[] { "open", "visible", "showShutdown", "showRestart", "showSleep", "showLogout", "showLock" }, new[] { "opened", "closed", "actionSelected" }, Actions: new[] { "Open", "Close" }),
        new ComponentContract("LockScreen", "Shell", new[] { "open", "clockFormat", "wallpaper", "visible", "showClock", "showDate", "unlockMethod", "autoLockTimeout" }, new[] { "opened", "closed", "unlocked" }, Actions: new[] { "Open", "Close", "Lock" }),
        new ComponentContract("Application", "Shell", new[] { "appId", "title", "icon", "running", "focused" }, new[] { "activated", "deactivated" }, Actions: new[] { "Launch", "Close", "Focus" }),
        new ComponentContract("WidgetHost", "Shell", new[] { "widgets", "layout", "visible" }, new[] { "widgetClicked" }, Actions: new[] { "AddWidget", "RemoveWidget" }),
        new ComponentContract("OSD", "Shell", new[] { "open", "message", "timeout", "icon" }, new[] { "opened", "closed" }, Actions: new[] { "Open", "Close", "Dismiss" }),
        new ComponentContract("Login", "Shell", new[] { "open", "username", "avatar", "hint" }, new[] { "submitted", "canceled" }, Actions: new[] { "Submit", "Cancel" }),
        new ComponentContract("AppGrid", "Shell", new[] { "apps", "columns", "iconSize" }, new[] { "appClicked" }, Actions: new[] { "Launch" }),
        new ComponentContract("Clock", "Shell", new[] { "format", "showSeconds" }, Array.Empty<string>()),
        new ComponentContract("Dock", "Shell", new[] { "position", "pinnedApps", "runningApps", "autoHide", "iconSize", "magnify" }, new[] { "appClicked" }, Actions: new[] { "Launch" }),
        new ComponentContract("TitleBar", "Shell", new[] { "title", "icon" }, new[] { "doubleClicked" }),
        // Data
        new ComponentContract("List", "Data", new[] { "items", "selectedIndex", "multiple" }, new[] { "selectionChanged", "itemActivated" }, Actions: new[] { "SetSelectedIndex" }),
        new ComponentContract("ListItem", "Data", new[] { "label", "icon", "selected", "enabled" }, new[] { "clicked", "activated" }),
        new ComponentContract("DataTable", "Data", new[] { "columns", "rows", "selectedRow" }, new[] { "selectionChanged", "cellActivated" }, Actions: new[] { "SetSelectedRow" }),
        new ComponentContract("TreeView", "Data", new[] { "nodes", "selectedNode", "expandedNodes" }, new[] { "selectionChanged", "nodeExpanded", "nodeCollapsed" }, Actions: new[] { "Expand", "Collapse" }),
        new ComponentContract("Menu", "Data", new[] { "items", "open" }, new[] { "opened", "closed", "itemClicked" }, Actions: new[] { "Open", "Close" }),
        new ComponentContract("MenuItem", "Data", new[] { "label", "icon", "shortcut", "enabled", "checked" }, new[] { "clicked" }),
        // Form
        new ComponentContract("Form", "Form", new[] { "fields", "submitLabel", "valid" }, new[] { "submitted", "reset" }, Actions: new[] { "Submit", "Reset" }),
        new ComponentContract("DatePicker", "Form", new[] { "value", "min", "max", "format" }, new[] { "changed", "submitted" }, Actions: new[] { "Clear" }),
        new ComponentContract("TimePicker", "Form", new[] { "value", "format" }, new[] { "changed", "submitted" }, Actions: new[] { "Clear" }),
        new ComponentContract("FilePicker", "Form", new[] { "value", "filter", "multiple", "folder" }, new[] { "selected", "canceled" }, Actions: new[] { "Open", "Clear" }),
        new ComponentContract("SettingsPanel", "Form", new[] { "sections", "selectedSection", "searchable" }, new[] { "sectionChanged" }, Actions: new[] { "SetSection", "Search" }),
        // Media
        new ComponentContract("Video", "Media", new[] { "source", "autoplay", "loop", "muted", "volume" }, new[] { "played", "paused", "ended", "error" }, Actions: new[] { "Play", "Pause", "Seek", "SetVolume" }),
        new ComponentContract("Audio", "Media", new[] { "source", "autoplay", "loop", "volume" }, new[] { "played", "paused", "ended", "error" }, Actions: new[] { "Play", "Pause", "Seek", "SetVolume" }),
        new ComponentContract("MediaPlayer", "Media", new[] { "source", "playing", "volume", "position", "muted" }, new[] { "played", "paused", "ended", "positionChanged", "error" }, Actions: new[] { "Play", "Pause", "Seek", "SetVolume" }),
        // Developer
        new ComponentContract("Terminal", "Developer", new[] { "command", "cwd", "history", "prompt" }, new[] { "commandEntered", "outputReady" }, Actions: new[] { "Write", "Clear", "RunCommand", "SetCwd" }),
        new ComponentContract("CodeEditor", "Developer", new[] { "value", "language", "readOnly", "theme", "lineNumbers" }, new[] { "changed", "cursorMoved" }, Actions: new[] { "SetValue", "SetLanguage", "Format" }),
        new ComponentContract("LogViewer", "Developer", new[] { "entries", "filter", "level", "follow" }, new[] { "filterChanged" }, Actions: new[] { "Clear", "SetFilter" }),
    };

    private static readonly Dictionary<string, ComponentContract> ByType =
        All.ToDictionary(c => c.Type, StringComparer.Ordinal);

    public static bool TryGet(string type, out ComponentContract? contract) =>
        ByType.TryGetValue(type, out contract);

    public static IEnumerable<ComponentContract> ByCategory(string category) =>
        All.Where(c => string.Equals(c.Category, category, StringComparison.Ordinal));
}
