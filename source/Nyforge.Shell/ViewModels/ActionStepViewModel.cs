using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// One step of a behavior's action chain (NUI-SCHEMA §7.3) in the
/// node-graph Logic Editor — a <see cref="NuiAction"/> with the same
/// contract-driven target/name pickers the flat editor used. Edits
/// mutate the model in place; the BehaviorViewModel owns the chain's
/// structure (add/remove/reorder).
/// </summary>
public sealed class ActionStepViewModel : ViewModelBase
{
    public NuiAction Model { get; }

    /// <summary>Resolves a component id -> its NUI type, so Name choices
    /// can be looked up per-target (same resolver the flat editor used).</summary>
    private readonly Func<string, string?> _resolveComponentType;

    /// <summary>The owning behavior — provides the chain's structural
    /// commands (add/remove/reorder), which the step row's buttons bind.
    /// Set by BehaviorViewModel.RebuildSteps.</summary>
    public BehaviorViewModel? Owner { get; set; }

    public ActionStepViewModel(NuiAction model, Func<string, string?> resolveComponentType)
    {
        Model = model;
        _resolveComponentType = resolveComponentType;
    }

    public string Target
    {
        get => Model.Target;
        set
        {
            Model.Target = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailableActionNames));
            OnPropertyChanged(nameof(Summary));
        }
    }

    public string Name
    {
        get => Model.Name;
        set { Model.Name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    /// <summary>Valid names for the current Target, per the anti-drift
    /// rule in NUI-SCHEMA.md §7 — the editor must not let you type an
    /// action name that doesn't exist on the target's contract.</summary>
    public IReadOnlyList<string> AvailableActionNames
    {
        get
        {
            if (Target == "System")
            {
                return NuiSystemActions.All.Select(a => a.Name).ToList();
            }

            var type = _resolveComponentType(Target);
            if (type is not null && ComponentContracts.TryGet(type, out var contract))
            {
                return contract!.Actions;
            }

            return Array.Empty<string>();
        }
    }

    public string Summary => $"{Target}.{Name}";
}
