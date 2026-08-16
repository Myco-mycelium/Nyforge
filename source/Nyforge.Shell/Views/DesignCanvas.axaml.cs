using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class DesignCanvas : UserControl
{
    private CanvasElementViewModel? _dragTarget;
    private Point _dragStartPointerPosition;
    private double _dragStartX, _dragStartY;

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
        if (_dragTarget is null) return;

        var current = e.GetPosition(RootCanvas);
        var deltaX = current.X - _dragStartPointerPosition.X;
        var deltaY = current.Y - _dragStartPointerPosition.Y;

        _dragTarget.X = Math.Max(0, _dragStartX + deltaX);
        _dragTarget.Y = Math.Max(0, _dragStartY + deltaY);
    }

    private void OnElementPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragTarget is not null)
        {
            e.Pointer.Capture(null);
            _dragTarget = null;
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
            _resizeTarget = null;
        }
    }
}
