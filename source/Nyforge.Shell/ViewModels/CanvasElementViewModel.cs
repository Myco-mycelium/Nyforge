using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Design-time wrapper around a NuiComponent: adds Selected state and
/// change notification the canvas view needs, without putting editor
/// concerns into Nyforge.Core (NFC-001 §5.1).
///
/// v0.1 scope note: the canvas renders and edits the top-level children of
/// the active screen's root only (no deep nested-tree editing UI yet). The
/// underlying NuiComponent.Children field fully supports nesting — a
/// hand-authored .nstudio file (see examples/settings-app) can and does use
/// it — the canvas UI for editing nested trees is v0.2 scope.
/// </summary>
public sealed class CanvasElementViewModel : ViewModelBase
{
    public NuiComponent Model { get; }

    public string Id => Model.Id;
    public string Type => Model.Type;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public double X
    {
        get => Model.Layout.X;
        set { Model.Layout.X = value; OnPropertyChanged(); }
    }

    public double Y
    {
        get => Model.Layout.Y;
        set { Model.Layout.Y = value; OnPropertyChanged(); }
    }

    public double Width
    {
        get => Model.Layout.Width;
        set { Model.Layout.Width = Math.Max(8, value); OnPropertyChanged(); }
    }

    public double Height
    {
        get => Model.Layout.Height;
        set { Model.Layout.Height = Math.Max(8, value); OnPropertyChanged(); }
    }

    public string DisplayText
    {
        get => Model.Properties.TryGetValue("text", out var v) ? v?.ToString() ?? Type : Type;
        set { Model.Properties["text"] = value; OnPropertyChanged(); }
    }

    public CanvasElementViewModel(NuiComponent model)
    {
        Model = model;
    }
}
