using System.Collections.ObjectModel;
using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// One animation in the timeline editor (NUI-SCHEMA §8.3) — wraps a
/// <see cref="NuiAnimation"/> plus its keyframe sub-list. The panel
/// edits these in place; the model stays in sync for Preview and
/// serialization.
/// </summary>
public sealed class AnimationViewModel : ViewModelBase
{
    public NuiAnimation Model { get; }

    public ObservableCollection<KeyframeViewModel> Keyframes { get; }

    public AnimationViewModel(NuiAnimation model)
    {
        Model = model;
        Keyframes = new ObservableCollection<KeyframeViewModel>(
            model.Keyframes.Select(k => new KeyframeViewModel(k)));
        AddKeyframeCommand = new RelayCommand(AddKeyframe);
        RemoveKeyframeCommand = new RelayCommand<KeyframeViewModel>(RemoveKeyframe, k => k is not null);
    }

    public string Id
    {
        get => Model.Id;
        set { Model.Id = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string Target
    {
        get => Model.Target;
        set { Model.Target = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string Property
    {
        get => Model.Property;
        set { Model.Property = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public int Duration
    {
        get => Model.Duration;
        set { Model.Duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public int Delay
    {
        get => Model.Delay;
        set { Model.Delay = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string Easing
    {
        get => Model.Easing;
        set { Model.Easing = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public IReadOnlyList<string> EasingOptions { get; } =
        new[] { "linear", "ease-in", "ease-out", "ease-in-out", "steps" };

    public int Repeat
    {
        get => Model.Repeat;
        set { Model.Repeat = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string Direction
    {
        get => Model.Direction;
        set { Model.Direction = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public IReadOnlyList<string> DirectionOptions { get; } =
        new[] { "forward", "reverse", "alternate" };

    public string Summary => $"{Id}: {Target}.{Property} ({Duration} ms, {Easing})";

    // ---- keyframe commands --------------------------------------------------

    public RelayCommand AddKeyframeCommand { get; }
    public RelayCommand<KeyframeViewModel> RemoveKeyframeCommand { get; }

    private void AddKeyframe()
    {
        var kf = new NuiKeyframe { Offset = 1.0, Value = 1.0 };
        Model.Keyframes.Add(kf);
        Keyframes.Add(new KeyframeViewModel(kf));
        OnPropertyChanged(nameof(Summary));
    }

    private void RemoveKeyframe(KeyframeViewModel? kf)
    {
        if (kf is null) return;
        Model.Keyframes.Remove(kf.Model);
        Keyframes.Remove(kf);
        OnPropertyChanged(nameof(Summary));
    }
}
