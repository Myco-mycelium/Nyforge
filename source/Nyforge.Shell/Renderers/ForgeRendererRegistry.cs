using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Renderers;

/// <summary>
/// Populates the Core <see cref="ComponentRendererRegistry"/> with
/// Forge's Avalonia-specific renderers. Called once at PreviewViewModel
/// construction; the PreviewWindow's DataTemplate selector reads the
/// registry instead of hardcoded converter sets (NFM-000 §2.1).
/// </summary>
public static class ForgeRendererRegistry
{
    /// <summary>
    /// Creates a pre-populated registry with all known Forge renderers.
    /// </summary>
    public static ComponentRendererRegistry Create()
    {
        var reg = new ComponentRendererRegistry();

        // ---- interactive (leaf) components ----
        reg.Register(new TextRenderer());
        reg.Register(new ButtonRenderer());
        reg.Register(new CheckboxRenderer());
        reg.Register(new ToggleRenderer());
        reg.Register(new LinkRenderer());
        reg.Register(new SliderRenderer());
        reg.Register(new ProgressBarRenderer());
        reg.Register(new IconRenderer());
        reg.Register(new ImageRenderer());
        reg.Register(new RadioRenderer());
        reg.Register(new InputRenderer());
        reg.Register(new PasswordFieldRenderer());
        reg.Register(new DatePickerRenderer());
        reg.Register(new TimePickerRenderer());
        reg.Register(new SearchRenderer());

        // ---- layout containers ----
        reg.Register(new ContainerRenderer());
        reg.Register(new StackRenderer());
        reg.Register(new GridRenderer());
        reg.Register(new FlexLayoutRenderer());
        reg.Register(new DockRenderer());
        reg.Register(new SplitViewRenderer());
        reg.Register(new ScrollViewRenderer());
        reg.Register(new TabsRenderer());
        reg.Register(new PanelRenderer());

        // ---- shell chrome ----
        reg.Register(new WindowFrameRenderer());
        reg.Register(new TitleBarRenderer());
        reg.Register(new WindowControlsRenderer());
        reg.Register(new TaskbarRenderer());
        reg.Register(new StartMenuRenderer());
        reg.Register(new SystemTrayRenderer());
        reg.Register(new ClockRenderer());
        reg.Register(new NotificationCenterRenderer());
        reg.Register(new QuickSettingsRenderer());
        reg.Register(new WorkspaceSwitcherRenderer());
        reg.Register(new DesktopSurfaceRenderer());
        reg.Register(new DesktopIconRenderer());
        reg.Register(new WidgetHostRenderer());
        reg.Register(new StatusBarRenderer());
        reg.Register(new SidebarRenderer());
        reg.Register(new ToolbarRenderer());
        reg.Register(new ContextMenuRenderer());
        reg.Register(new CommandPaletteRenderer());
        reg.Register(new MenuRenderer());
        reg.Register(new MenuItemRenderer());
        reg.Register(new ListRenderer());
        reg.Register(new ListItemRenderer());
        reg.Register(new BreadcrumbsRenderer());
        reg.Register(new NavigationRenderer());
        reg.Register(new NavigationRailRenderer());
        reg.Register(new OSDRenderer());
        reg.Register(new LockScreenRenderer());
        reg.Register(new PowerMenuRenderer());
        reg.Register(new LoginRenderer());
        reg.Register(new DialogRenderer());
        reg.Register(new NotificationRenderer());
        reg.Register(new CardRenderer());
        reg.Register(new TreeViewRenderer());
        reg.Register(new DataTableRenderer());
        reg.Register(new FormRenderer());
        reg.Register(new SettingsPanelRenderer());
        reg.Register(new LauncherRenderer());
        reg.Register(new FilePickerRenderer());
        reg.Register(new CodeEditorRenderer());
        reg.Register(new TerminalRenderer());
        reg.Register(new LogViewerRenderer());
        reg.Register(new MediaPlayerRenderer());
        reg.Register(new AudioRenderer());
        reg.Register(new VideoRenderer());

        return reg;
    }
}
