using Nyforge.Core.Nui;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Renderers;

// ---- Taskbar ----
public sealed class TaskbarRenderer : IEventRenderer
{
    public string Name => "Taskbar";
    public IReadOnlyList<string> SupportedTypes => new[] { "Taskbar" };
    public IReadOnlyList<string> SupportedEvents => new[] { "appPinned", "appUnpinned" };
}

// ---- StartMenu ----
public sealed class StartMenuRenderer : IEventRenderer
{
    public string Name => "StartMenu";
    public IReadOnlyList<string> SupportedTypes => new[] { "StartMenu" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed" };
}

// ---- SystemTray ----
public sealed class SystemTrayRenderer : IEventRenderer
{
    public string Name => "SystemTray";
    public IReadOnlyList<string> SupportedTypes => new[] { "SystemTray" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked" };
}

// ---- Clock ----
public sealed class ClockRenderer : IComponentRenderer
{
    public string Name => "Clock";
    public IReadOnlyList<string> SupportedTypes => new[] { "Clock" };
}

// ---- NotificationCenter ----
public sealed class NotificationCenterRenderer : IEventRenderer
{
    public string Name => "NotificationCenter";
    public IReadOnlyList<string> SupportedTypes => new[] { "NotificationCenter" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed", "dismissed" };
}

// ---- QuickSettings ----
public sealed class QuickSettingsRenderer : IEventRenderer
{
    public string Name => "QuickSettings";
    public IReadOnlyList<string> SupportedTypes => new[] { "QuickSettings" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed" };
}

// ---- WorkspaceSwitcher ----
public sealed class WorkspaceSwitcherRenderer : IEventRenderer
{
    public string Name => "WorkspaceSwitcher";
    public IReadOnlyList<string> SupportedTypes => new[] { "WorkspaceSwitcher" };
    public IReadOnlyList<string> SupportedEvents => new[] { "workspaceChanged" };
}

// ---- DesktopSurface ----
public sealed class DesktopSurfaceRenderer : ILayoutRenderer
{
    public string Name => "DesktopSurface";
    public IReadOnlyList<string> SupportedTypes => new[] { "DesktopSurface" };
    public bool IsContainer => true;
}

// ---- DesktopIcon ----
public sealed class DesktopIconRenderer : IEventRenderer
{
    public string Name => "DesktopIcon";
    public IReadOnlyList<string> SupportedTypes => new[] { "DesktopIcon" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked", "doubleClicked" };
}

// ---- WidgetHost ----
public sealed class WidgetHostRenderer : ILayoutRenderer
{
    public string Name => "WidgetHost";
    public IReadOnlyList<string> SupportedTypes => new[] { "WidgetHost" };
    public bool IsContainer => true;
}

// ---- StatusBar ----
public sealed class StatusBarRenderer : IComponentRenderer
{
    public string Name => "StatusBar";
    public IReadOnlyList<string> SupportedTypes => new[] { "StatusBar" };
}

// ---- Sidebar ----
public sealed class SidebarRenderer : ILayoutRenderer
{
    public string Name => "Sidebar";
    public IReadOnlyList<string> SupportedTypes => new[] { "Sidebar" };
    public bool IsContainer => true;
}

// ---- Toolbar ----
public sealed class ToolbarRenderer : IComponentRenderer
{
    public string Name => "Toolbar";
    public IReadOnlyList<string> SupportedTypes => new[] { "Toolbar" };
}

// ---- ContextMenu ----
public sealed class ContextMenuRenderer : IEventRenderer
{
    public string Name => "ContextMenu";
    public IReadOnlyList<string> SupportedTypes => new[] { "ContextMenu" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed" };
}

// ---- CommandPalette ----
public sealed class CommandPaletteRenderer : IEventRenderer
{
    public string Name => "CommandPalette";
    public IReadOnlyList<string> SupportedTypes => new[] { "CommandPalette" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed", "submitted" };
}

// ---- Menu ----
public sealed class MenuRenderer : ILayoutRenderer
{
    public string Name => "Menu";
    public IReadOnlyList<string> SupportedTypes => new[] { "Menu" };
    public bool IsContainer => true;
}

// ---- MenuItem ----
public sealed class MenuItemRenderer : IEventRenderer
{
    public string Name => "MenuItem";
    public IReadOnlyList<string> SupportedTypes => new[] { "MenuItem" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked" };
}

// ---- List ----
public sealed class ListRenderer : ILayoutRenderer
{
    public string Name => "List";
    public IReadOnlyList<string> SupportedTypes => new[] { "List" };
    public bool IsContainer => true;
}

// ---- ListItem ----
public sealed class ListItemRenderer : IEventRenderer
{
    public string Name => "ListItem";
    public IReadOnlyList<string> SupportedTypes => new[] { "ListItem" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked", "selected" };
}

// ---- Breadcrumbs ----
public sealed class BreadcrumbsRenderer : IEventRenderer
{
    public string Name => "Breadcrumbs";
    public IReadOnlyList<string> SupportedTypes => new[] { "Breadcrumbs" };
    public IReadOnlyList<string> SupportedEvents => new[] { "clicked" };
}

// ---- Navigation ----
public sealed class NavigationRenderer : IEventRenderer
{
    public string Name => "Navigation";
    public IReadOnlyList<string> SupportedTypes => new[] { "Navigation" };
    public IReadOnlyList<string> SupportedEvents => new[] { "navigated" };
}

// ---- NavigationRail ----
public sealed class NavigationRailRenderer : IEventRenderer
{
    public string Name => "NavigationRail";
    public IReadOnlyList<string> SupportedTypes => new[] { "NavigationRail" };
    public IReadOnlyList<string> SupportedEvents => new[] { "navigated" };
}

// ---- OSD ----
public sealed class OSDRenderer : IComponentRenderer
{
    public string Name => "OSD";
    public IReadOnlyList<string> SupportedTypes => new[] { "OSD" };
}

// ---- LockScreen ----
public sealed class LockScreenRenderer : IEventRenderer
{
    public string Name => "LockScreen";
    public IReadOnlyList<string> SupportedTypes => new[] { "LockScreen" };
    public IReadOnlyList<string> SupportedEvents => new[] { "unlocked", "submitted" };
}

// ---- PowerMenu ----
public sealed class PowerMenuRenderer : IEventRenderer
{
    public string Name => "PowerMenu";
    public IReadOnlyList<string> SupportedTypes => new[] { "PowerMenu" };
    public IReadOnlyList<string> SupportedEvents => new[] { "shutdown", "restart", "sleep", "lock" };
}

// ---- Login ----
public sealed class LoginRenderer : IEventRenderer
{
    public string Name => "Login";
    public IReadOnlyList<string> SupportedTypes => new[] { "Login" };
    public IReadOnlyList<string> SupportedEvents => new[] { "submitted", "cancelled" };
}

// ---- Dialog ----
public sealed class DialogRenderer : IEventRenderer
{
    public string Name => "Dialog";
    public IReadOnlyList<string> SupportedTypes => new[] { "Dialog" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed", "submitted", "cancelled" };
}

// ---- Card ----
public sealed class CardRenderer : ILayoutRenderer
{
    public string Name => "Card";
    public IReadOnlyList<string> SupportedTypes => new[] { "Card" };
    public bool IsContainer => true;
}

// ---- TreeView ----
public sealed class TreeViewRenderer : IEventRenderer
{
    public string Name => "TreeView";
    public IReadOnlyList<string> SupportedTypes => new[] { "TreeView" };
    public IReadOnlyList<string> SupportedEvents => new[] { "selected", "expanded", "collapsed" };
}

// ---- DataTable ----
public sealed class DataTableRenderer : IEventRenderer
{
    public string Name => "DataTable";
    public IReadOnlyList<string> SupportedTypes => new[] { "DataTable" };
    public IReadOnlyList<string> SupportedEvents => new[] { "selected", "sorted" };
}

// ---- Form ----
public sealed class FormRenderer : IEventRenderer
{
    public string Name => "Form";
    public IReadOnlyList<string> SupportedTypes => new[] { "Form" };
    public IReadOnlyList<string> SupportedEvents => new[] { "submitted", "cancelled" };
}

// ---- SettingsPanel ----
public sealed class SettingsPanelRenderer : ILayoutRenderer
{
    public string Name => "SettingsPanel";
    public IReadOnlyList<string> SupportedTypes => new[] { "SettingsPanel" };
    public bool IsContainer => true;
}

// ---- Launcher ----
public sealed class LauncherRenderer : IEventRenderer
{
    public string Name => "Launcher";
    public IReadOnlyList<string> SupportedTypes => new[] { "Launcher" };
    public IReadOnlyList<string> SupportedEvents => new[] { "Opened", "Closed", "launched" };
}

// ---- FilePicker ----
public sealed class FilePickerRenderer : IEventRenderer
{
    public string Name => "FilePicker";
    public IReadOnlyList<string> SupportedTypes => new[] { "FilePicker" };
    public IReadOnlyList<string> SupportedEvents => new[] { "selected", "cancelled" };
}

// ---- CodeEditor ----
public sealed class CodeEditorRenderer : IEventRenderer
{
    public string Name => "CodeEditor";
    public IReadOnlyList<string> SupportedTypes => new[] { "CodeEditor" };
    public IReadOnlyList<string> SupportedEvents => new[] { "changed", "saved" };
}

// ---- Terminal ----
public sealed class TerminalRenderer : IEventRenderer
{
    public string Name => "Terminal";
    public IReadOnlyList<string> SupportedTypes => new[] { "Terminal" };
    public IReadOnlyList<string> SupportedEvents => new[] { "output", "command" };
}

// ---- LogViewer ----
public sealed class LogViewerRenderer : IComponentRenderer
{
    public string Name => "LogViewer";
    public IReadOnlyList<string> SupportedTypes => new[] { "LogViewer" };
}

// ---- MediaPlayer ----
public sealed class MediaPlayerRenderer : IEventRenderer
{
    public string Name => "MediaPlayer";
    public IReadOnlyList<string> SupportedTypes => new[] { "MediaPlayer" };
    public IReadOnlyList<string> SupportedEvents => new[] { "play", "pause", "seek", "ended" };
}

// ---- Audio ----
public sealed class AudioRenderer : IEventRenderer
{
    public string Name => "Audio";
    public IReadOnlyList<string> SupportedTypes => new[] { "Audio" };
    public IReadOnlyList<string> SupportedEvents => new[] { "play", "pause", "ended" };
}

// ---- Video ----
public sealed class VideoRenderer : IEventRenderer
{
    public string Name => "Video";
    public IReadOnlyList<string> SupportedTypes => new[] { "Video" };
    public IReadOnlyList<string> SupportedEvents => new[] { "play", "pause", "ended" };
}

// ---- WindowFrame ----
public sealed class WindowFrameRenderer : ILayoutRenderer
{
    public string Name => "WindowFrame";
    public IReadOnlyList<string> SupportedTypes => new[] { "WindowFrame" };
    public bool IsContainer => true;
}

// ---- TitleBar ----
public sealed class TitleBarRenderer : IComponentRenderer
{
    public string Name => "TitleBar";
    public IReadOnlyList<string> SupportedTypes => new[] { "TitleBar" };
}

// ---- WindowControls ----
public sealed class WindowControlsRenderer : IEventRenderer
{
    public string Name => "WindowControls";
    public IReadOnlyList<string> SupportedTypes => new[] { "WindowControls" };
    public IReadOnlyList<string> SupportedEvents => new[] { "minimize", "maximize", "close", "restore" };
}

// ---- Shell ----
public sealed class ShellRenderer : ILayoutRenderer
{
    public string Name => "Shell";
    public IReadOnlyList<string> SupportedTypes => new[] { "Shell" };
    public bool IsContainer => true;
}
