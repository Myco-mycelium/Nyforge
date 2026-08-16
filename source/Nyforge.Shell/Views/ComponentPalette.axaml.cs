using Avalonia.Controls;
using Avalonia.Input;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class ComponentPalette : UserControl
{
    public ComponentPalette()
    {
        InitializeComponent();
    }

    /// <summary>
    /// v0.1 uses double-click-to-add rather than a true pointer drag-drop
    /// gesture from palette to canvas. The canvas itself fully supports
    /// dragging once an element exists on it (see DesignCanvas.axaml.cs) —
    /// palette-to-canvas drag tracking is left for v0.2 alongside the
    /// deeper interaction work (alignment guides, snap-to-grid).
    /// </summary>
    private void OnPaletteItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { Tag: string componentType } && DataContext is MainWindowViewModel vm)
        {
            vm.AddComponentCommand.Execute(componentType);
        }
    }
}
