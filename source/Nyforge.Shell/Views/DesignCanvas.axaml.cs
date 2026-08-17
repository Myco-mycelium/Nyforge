using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nyforge.Core.Editing;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class DesignCanvas : UserControl
{
    private CanvasElementViewModel? _dragTarget;
    private Point _dragStartPointerPosition;
    private double _dragStartX, _dragStartY;
    private CanvasElementViewModel? _dropTargetHighlight;

    private CanvasElementViewModel? _resizeTarget;
    private Point _resizeStartPointerPosition;
    private double _resizeStartWidth, _resizeStartHeight;

    public DesignCanvas()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    /// <summary>Clicking empty canvas clears selection.</summary>
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Canvas && Vm is not null)
        {
            Vm.SelectedElement = null;
        }
    }

    private static CanvasElementViewModel? ElementFromSender(object? sender) =>
        (sender as Control)?.DataContext as CanvasElementViewModel;

    // --- Move ---

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var element = ElementFromSender(sender);
        if (element is null || Vm is null) return;

        Vm.SelectedElement = element;
        _dragTarget = element;
        _dragStartPointerPosition = e.GetPosition(RootCanvas);
        _dragStartX = element.X;
        _dragStartY = element.Y;
        e.Pointer.Capture(sender as Control);
        e.Handled = true;
    }

    private void OnElementPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragTarget is null || Vm is null) return;

        var current = e.GetPosition(RootCanvas);
        var deltaX = current.X - _dragStartPointerPosition.X;
        var deltaY = current.Y - _dragStartPointerPosition.Y;

        // Relative model position (clamped inside the parent), then the
        // absolute render position follows via the parent chain.
        _dragTarget.X = Math.Max(0, _dragStartX + deltaX);
        _dragTarget.Y = Math.Max(0, _dragStartY + deltaY);
        Vm.RefreshRenderPositions();

        // v0.6 drop affordance: highlight the container we'd reparent into.
        var hover = Vm.ContainerAt(current.X, current.Y, _dragTarget);
        if (hover != _dropTargetHighlight)
        {
            if (_dropTargetHighlight is not null) _dropTargetHighlight.IsSelected = false;
            _dropTargetHighlight = hover;
            if (_dropTargetHighlight is not null) _dropTargetHighlight.IsSelected = true;
        }
    }

    private void OnElementPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragTarget is null) return;

        var element = _dragTarget;
        e.Pointer.Capture(null);
        _dragTarget = null;

        if (_dropTargetHighlight is not null)
        {
            _dropTargetHighlight.IsSelected = false;
            _dropTargetHighlight = null;
        }

        // v0.6: dropping on a container reparents into it; dropping on
        // empty canvas pops the element out to the screen root.
        if (Vm is not null)
        {
            var current = e.GetPosition(RootCanvas);
            var target = Vm.ContainerAt(current.X, current.Y, element);
            var reparented = Vm.TryReparent(element, target);

            // One gesture, one command (undo/redo architecture, item #5):
            // if the drop didn't reparent — whose single command already
            // captured the whole move — commit the drag as ONE
            // MoveComponentCommand. The pointer moves themselves are never
            // recorded individually.
            if (!reparented && (element.X != _dragStartX || element.Y != _dragStartY))
            {
                Vm.History.Execute(new MoveComponentCommand(
                    element.Model, _dragStartX, _dragStartY, element.X, element.Y));
            }
        }
    }

    // --- Resize ---

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var element = ElementFromSender(sender);
        if (element is null) return;

        _resizeTarget = element;
        _resizeStartPointerPosition = e.GetPosition(RootCanvas);
        _resizeStartWidth = element.Width;
        _resizeStartHeight = element.Height;
        e.Pointer.Capture(sender as Control);
        e.Handled = true; // prevent the underlying element's move handler from also firing
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_resizeTarget is null) return;

        var current = e.GetPosition(RootCanvas);
        var deltaX = current.X - _resizeStartPointerPosition.X;
        var deltaY = current.Y - _resizeStartPointerPosition.Y;

        _resizeTarget.Width = _resizeStartWidth + deltaX;
        _resizeTarget.Height = _resizeStartHeight + deltaY;
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_resizeTarget is not null)
        {
            e.Pointer.Capture(null);
            if (Vm is not null &&
                (_resizeTarget.Width != _resizeStartWidth || _resizeTarget.Height != _resizeStartHeight))
            {
                // One command per completed resize gesture (see the move
                // handler's comment).
                Vm.History.Execute(new ResizeComponentCommand(
                    _resizeTarget.Model, _resizeStartWidth, _resizeStartHeight,
                    _resizeTarget.Width, _resizeTarget.Height));
            }
            _resizeTarget = null;
        }
    }
}
