using Nyforge.Core.Nui;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Renderers;

// ---- Container ----
public sealed class ContainerRenderer : ILayoutRenderer
{
    public string Name => "Container";
    public IReadOnlyList<string> SupportedTypes => new[] { "Container" };
    public bool IsContainer => true;
}

// ---- Stack ----
public sealed class StackRenderer : ILayoutRenderer
{
    public string Name => "Stack";
    public IReadOnlyList<string> SupportedTypes => new[] { "Stack" };
    public bool IsContainer => true;
}

// ---- Grid ----
public sealed class GridRenderer : ILayoutRenderer
{
    public string Name => "Grid";
    public IReadOnlyList<string> SupportedTypes => new[] { "Grid" };
    public bool IsContainer => true;
}

// ---- FlexLayout ----
public sealed class FlexLayoutRenderer : ILayoutRenderer
{
    public string Name => "FlexLayout";
    public IReadOnlyList<string> SupportedTypes => new[] { "FlexLayout" };
    public bool IsContainer => true;
}

// ---- Dock ----
public sealed class DockRenderer : ILayoutRenderer
{
    public string Name => "Dock";
    public IReadOnlyList<string> SupportedTypes => new[] { "Dock" };
    public bool IsContainer => true;
}

// ---- SplitView ----
public sealed class SplitViewRenderer : ILayoutRenderer
{
    public string Name => "SplitView";
    public IReadOnlyList<string> SupportedTypes => new[] { "SplitView" };
    public bool IsContainer => true;
}

// ---- ScrollView ----
public sealed class ScrollViewRenderer : ILayoutRenderer
{
    public string Name => "ScrollView";
    public IReadOnlyList<string> SupportedTypes => new[] { "ScrollView" };
    public bool IsContainer => true;
}

// ---- Tabs ----
public sealed class TabsRenderer : ILayoutRenderer
{
    public string Name => "Tabs";
    public IReadOnlyList<string> SupportedTypes => new[] { "Tabs" };
    public bool IsContainer => true;
}

// ---- Panel ----
public sealed class PanelRenderer : ILayoutRenderer
{
    public string Name => "Panel";
    public IReadOnlyList<string> SupportedTypes => new[] { "Panel" };
    public bool IsContainer => true;
}

// ---- Window ----
public sealed class WindowRenderer : ILayoutRenderer
{
    public string Name => "Window";
    public IReadOnlyList<string> SupportedTypes => new[] { "Window" };
    public bool IsContainer => true;
}
