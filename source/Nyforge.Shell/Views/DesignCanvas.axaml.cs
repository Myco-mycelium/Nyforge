using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Nyforge.Core.Canvas;
using Nyforge.Core.Editing;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class DesignCanvas : UserControl
{
    private CanvasElementViewModel? _dragTarget;
    private Point _dragStartPointerPosition;
    private double _dragStartX, _dragStartY;
    private CanvasElementViewModel? _dropTargetHighlight;

    // Multi-select drag state: all topmost selected elements and their
    // start positions. Children of a selected container ride along with it.
    private bool _multiDrag;
    private Dictionary<CanvasElementViewModel, (double X, double Y)>? _multiDragStarts;

    private CanvasElementViewModel? _resizeTarget;
    private Point _resizeStartPointerPosition;
    private double _resizeStartWidth, _resizeStartHeight;

    // Alignment guides
    private readonly AlignmentGuideService _guides = new();
    private double? _guideX;  // vertical guide line X position (null = no guide)
    private double? _guideY;  // horizontal guide line Y position (null = no guide)

    // Zoom: ScaleTransform found from RootCanvas after InitializeComponent
    private ScaleTransform? _canvasScale;

    public DesignCanvas()
    {
        InitializeComponent();
        _canvasScale = RootCanvas.RenderTransform as ScaleTransform;
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

    /// <summary>Snap to the design system's 4 px grid (docs/reference/design-system.md).</summary>
    private static double Snap(double v) => Math.Round(v / 4.0) * 4.0;

    // --- Selection + Move ---

    private void OnElementPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var element = ElementFromSender(sender);
        if (element is null || Vm is null) return;

        // Ctrl/Cmd-click toggles membership — a toggle never starts a drag.
        var additive = e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
                       e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        if (additive)
        {
            Vm.SelectForInteraction(element, additive: true);
            e.Handled = true;
            return;
        }

        if (Vm.SelectedElements.Contains(element) && Vm.SelectedElements.Count > 1)
        {
            // Press on a member of a multi-selection: keep the selection and
            // drag them all; collapse to a single selection on a click that
            // doesn't move (standard design-tool behavior).
            _multiDrag = true;
        }
        else
        {
            Vm.SelectForInteraction(element, additive: false);
        }

        _dragTarget = element;
        _dragStartPointerPosition = e.GetPosition(RootCanvas);
        _dragStartX = element.X;
        _dragStartY = element.Y;
        _multiDragStarts = Vm.TopmostSelected().ToDictionary(vm => vm, vm => (vm.X, vm.Y));
        e.Pointer.Capture(sender as Control);
        e.Handled = true;
    }

    private void OnElementPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragTarget is null || Vm is null) return;

        var current = e.GetPosition(RootCanvas);
        var deltaX = current.X - _dragStartPointerPosition.X;
        var deltaY = current.Y - _dragStartPointerPosition.Y;

        // Build sibling bounds for alignment guides (exclude the dragged element)
        var siblings = new List<AlignmentBounds>();
        foreach (var el in Vm.CanvasElements)
        {
            if (el == _dragTarget) continue;
            siblings.Add(new AlignmentBounds(el.X, el.Y, el.Width, el.Height));
        }

        if (_multiDrag && _multiDragStarts is not null)
        {
            foreach (var (vm, start) in _multiDragStarts)
            {
                var rawX = Math.Max(0, start.X + deltaX);
                var rawY = Math.Max(0, start.Y + deltaY);
                var snapX = _guides.SnapX(rawX, vm.Width, current.X, siblings, RootCanvas.Bounds.Width);
                var snapY = _guides.SnapY(rawY, vm.Height, current.Y, siblings, RootCanvas.Bounds.Height);
                vm.X = snapX?.Position ?? Snap(rawX);
                vm.Y = snapY?.Position ?? Snap(rawY);
            }
            _guideX = null;
            _guideY = null;
        }
        else
        {
            var rawX = Math.Max(0, _dragStartX + deltaX);
            var rawY = Math.Max(0, _dragStartY + deltaY);
            var snapX = _guides.SnapX(rawX, _dragTarget.Width, current.X, siblings, RootCanvas.Bounds.Width);
            var snapY = _guides.SnapY(rawY, _dragTarget.Height, current.Y, siblings, RootCanvas.Bounds.Height);
            _dragTarget.X = snapX?.Position ?? Snap(rawX);
            _dragTarget.Y = snapY?.Position ?? Snap(rawY);
            _guideX = snapX?.GuideLine;
            _guideY = snapY?.GuideLine;
        }
        Vm.RefreshRenderPositions();

        // Drop affordance only for single drags — multi-drags don't
        // reparent or reorder. A sibling is a reorder target; a container
        // is a reparent target.
        if (!_multiDrag)
        {
            var hover = Vm.SiblingAt(current.X, current.Y, _dragTarget)
                        ?? Vm.ContainerAt(current.X, current.Y, _dragTarget);
            if (hover != _dropTargetHighlight)
            {
                if (_dropTargetHighlight is not null) _dropTargetHighlight.IsSelected = false;
                _dropTargetHighlight = hover;
                if (_dropTargetHighlight is not null) _dropTargetHighlight.IsSelected = true;
            }
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

        // Clear alignment guide lines
        _guideX = null;
        _guideY = null;

        if (Vm is null) return;
        var current = e.GetPosition(RootCanvas);

        if (_multiDrag)
        {
            var starts = _multiDragStarts!;
            _multiDrag = false;
            _multiDragStarts = null;

            var moved = starts
                .Where(kv => kv.Key.X != kv.Value.X || kv.Key.Y != kv.Value.Y)
                .ToList();

            if (moved.Count == 0)
            {
                // Click without drag on a multi-selection collapses to the
                // pressed element.
                if (Vm.SelectedElements.Count > 1)
                {
                    Vm.SelectForInteraction(element, additive: false);
                }
            }
            else
            {
                // One gesture, one (composite) command — see item #5.
                var commands = moved
                    .Select(kv => (IEditorCommand)new MoveComponentCommand(
                        kv.Key.Model, kv.Value.X, kv.Value.Y, kv.Key.X, kv.Key.Y))
                    .ToList();
                if (commands.Count == 1)
                {
                    Vm.History.Execute(commands[0]);
                }
                else
                {
                    Vm.History.Execute(new CompositeCommand(commands));
                }
                Vm.StatusMessage = $"Moved {commands.Count} element{(commands.Count == 1 ? "" : "s")}.";
            }
            return;
        }

        // Single drag: dropping on a sibling reorders (z-order); dropping
        // on a container reparents into it; dropping on empty canvas pops
        // the element out to the screen root.
        var sibling = Vm.SiblingAt(current.X, current.Y, element);
        if (sibling is not null)
        {
            Vm.TryReorder(element, sibling);
        }
        else
        {
            var target = Vm.ContainerAt(current.X, current.Y, element);
            var reparented = Vm.TryReparent(element, target);
            if (!reparented && (element.X != _dragStartX || element.Y != _dragStartY))
            {
                // One gesture, one command (item #5): the drag itself is
                // never recorded per pointer-move.
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

        var w = Snap(Math.Max(8, _resizeStartWidth + deltaX));
        var h = Snap(Math.Max(8, _resizeStartHeight + deltaY));

        // Shift locks the aspect ratio: derive the secondary dimension
        // from whichever axis moved more.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _resizeStartHeight > 0)
        {
            var ratio = _resizeStartWidth / _resizeStartHeight;
            if (Math.Abs(deltaX) >= Math.Abs(deltaY)) h = Snap(Math.Max(8, w / ratio));
            else w = Snap(Math.Max(8, h * ratio));
        }

        _resizeTarget.Width = w;
        _resizeTarget.Height = h;
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

    // --- Zoom ---

    private double _zoomLevel = 1.0;
    private const double ZoomStep = 0.1;
    private const double ZoomMin = 0.25;
    private const double ZoomMax = 4.0;

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) ||
            e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            var delta = e.Delta.Y > 0 ? ZoomStep : -ZoomStep;
            SetZoom(_zoomLevel + delta);
            e.Handled = true;
        }
    }

    public void SetZoom(double level)
    {
        _zoomLevel = Math.Clamp(level, ZoomMin, ZoomMax);
        if (_canvasScale is not null)
        {
            _canvasScale.ScaleX = _zoomLevel;
            _canvasScale.ScaleY = _zoomLevel;
        }
        if (Vm is not null)
        {
            Vm.StatusMessage = $"Zoom: {Math.Round(_zoomLevel * 100)}%";
        }
    }

    public void ZoomIn() => SetZoom(_zoomLevel + ZoomStep);
    public void ZoomOut() => SetZoom(_zoomLevel - ZoomStep);
    public void ZoomReset() => SetZoom(1.0);
    public void ZoomFit()
    {
        if (Vm is null) return;
        var bounds = RootCanvas.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var maxW = Vm.CanvasRenderItems.Count > 0 ? Vm.CanvasRenderItems.Max(vm => vm.RenderX + vm.Width) : 1024;
        var maxH = Vm.CanvasRenderItems.Count > 0 ? Vm.CanvasRenderItems.Max(vm => vm.RenderY + vm.Height) : 768;
        var scaleX = bounds.Width / maxW;
        var scaleY = bounds.Height / maxH;
        SetZoom(Math.Min(scaleX, scaleY) * 0.9); // 90% margin
    }
}
