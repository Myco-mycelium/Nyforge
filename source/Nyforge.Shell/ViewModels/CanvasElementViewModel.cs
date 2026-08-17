using System.Collections.ObjectModel;
using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Design-time wrapper around a NuiComponent: adds Selected state and
/// change notification the canvas view needs, without putting editor
/// concerns into Nyforge.Core (NFC-001 §5.1).
///
/// v0.6: tree-aware. Each VM mirrors one node of the component tree —
/// <see cref="Parent"/> and <see cref="Children"/> form the hierarchy the
/// Layers panel shows, and <see cref="RenderX"/>/<see cref="RenderY"/>
/// hold the absolute canvas position (parent offsets summed up the chain;
/// the model's Layout stays relative to the parent, per NUI-SCHEMA §3).
/// MainWindowViewModel keeps model tree and VM tree in sync and refreshes
/// render positions after any structural change.
/// </summary>
public sealed class CanvasElementViewModel : ViewModelBase
{
    public NuiComponent Model { get; }

    /// <summary>The VM wrapping the model's parent node; null for top-level children of the screen root.</summary>
    public CanvasElementViewModel? Parent { get; set; }

    public string Id => Model.Id;
    public string Type => Model.Type;

    /// <summary>Depth in the component tree: 0 for top-level children of the screen root.</summary>
    public int Depth => Parent is null ? 0 : Parent.Depth + 1;

    /// <summary>True if this type can hold child components (NUI-SCHEMA §3).</summary>
    public bool CanContainChildren => ComponentTree.CanContainChildren(Type);

    /// <summary>Nested children — drives the Layers tree and reparent targets.</summary>
    public ObservableCollection<CanvasElementViewModel> Children { get; } = new();

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    // Relative (model) position within the parent.
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

    // Absolute canvas position — recomputed by MainWindowViewModel.
    private double _renderX, _renderY;
    public double RenderX
    {
        get => _renderX;
        set => SetField(ref _renderX, value);
    }

    public double RenderY
    {
        get => _renderY;
        set => SetField(ref _renderY, value);
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

    public CanvasElementViewModel(NuiComponent model, CanvasElementViewModel? parent = null)
    {
        Model = model;
        Parent = parent;
    }
}
