using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// One keyframe of an animation's multi-point curve (NUI-SCHEMA §8.3)
/// in the timeline editor — wraps a <see cref="NuiKeyframe"/>. The
/// offset is a normalized time in [0, 1] and the value is the target
/// component property's value there.
/// </summary>
public sealed class KeyframeViewModel : ViewModelBase
{
    public NuiKeyframe Model { get; }

    public KeyframeViewModel(NuiKeyframe model) => Model = model;

    public double Offset
    {
        get => Model.Offset;
        set { Model.Offset = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string Value
    {
        get => Model.Value?.ToString() ?? string.Empty;
        set
        {
            Model.Value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Summary));
        }
    }

    public string Summary => $"{Offset:0.##} → {Value}";
}
