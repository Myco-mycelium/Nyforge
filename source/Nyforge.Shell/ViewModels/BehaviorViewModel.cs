using System.Collections.ObjectModel;
using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Design-time wrapper around a NuiBehavior plus the component/event pair
/// it's currently bound to (a behavior is only reachable via some
/// component's Events dict — see NUI-SCHEMA.md §7). Editor-only concern,
/// kept out of Nyforge.Core per NFC-001 §5.1.
///
/// The node-graph Logic Editor surfaces the full NUI-SCHEMA §7.3 model:
/// a recursively-nested AND/OR condition tree
/// (<see cref="ConditionRoot"/> / <see cref="ConditionNodeViewModel"/>)
/// and an ordered action chain (<see cref="Steps"/>). Structural edits
/// migrate between the single-`action` and `actions`-chain forms so the
/// serializer stays noise-free (a lone step keeps the single form).
/// </summary>
public sealed class BehaviorViewModel : ViewModelBase
{
    public NuiBehavior Model { get; }

    /// <summary>The component this behavior is currently wired to fire from.</summary>
    public NuiComponent SourceComponent { get; }

    /// <summary>The event name on SourceComponent that triggers this behavior.</summary>
    public string EventName { get; }

    /// <summary>Resolves a component id -> its NUI type, so ActionName choices can be looked up per-target. Supplied by MainWindowViewModel.</summary>
    private readonly Func<string, string?> _resolveComponentType;

    public BehaviorViewModel(NuiBehavior model, NuiComponent sourceComponent, string eventName, Func<string, string?> resolveComponentType)
    {
        Model = model;
        SourceComponent = sourceComponent;
        EventName = eventName;
        _resolveComponentType = resolveComponentType;

        AddConditionCommand = new RelayCommand(AddCondition);
        RemoveConditionCommand = new RelayCommand(RemoveCondition);
        AddStepCommand = new RelayCommand(AddStep);
        RemoveStepCommand = new RelayCommand<ActionStepViewModel>(RemoveStep, s => s is not null);
        MoveStepUpCommand = new RelayCommand<ActionStepViewModel>(MoveStepUp, s => s is not null);
        MoveStepDownCommand = new RelayCommand<ActionStepViewModel>(MoveStepDown, s => s is not null);

        Steps = new ObservableCollection<ActionStepViewModel>();
        RebuildSteps();
        RebuildConditionRoot();
    }

    // ---- condition tree -----------------------------------------------------

    /// <summary>The root of the condition graph; null = the behavior
    /// always runs.</summary>
    public ConditionNodeViewModel? ConditionRoot { get; private set; }

    public RelayCommand AddConditionCommand { get; }
    public RelayCommand RemoveConditionCommand { get; }

    private void RebuildConditionRoot()
    {
        if (ConditionRoot is not null)
        {
            ConditionRoot.Changed -= OnConditionChanged;
        }
        ConditionRoot = Model.Condition is { } c
            ? new ConditionNodeViewModel(c, null)
            : null;
        if (ConditionRoot is not null)
        {
            ConditionRoot.Changed += OnConditionChanged;
        }
        OnPropertyChanged(nameof(ConditionRoot));
        OnPropertyChanged(nameof(Summary));
    }

    private void OnConditionChanged() => OnPropertyChanged(nameof(Summary));

    /// <summary>Starts a condition (a fresh leaf) when none exists.</summary>
    private void AddCondition()
    {
        if (Model.Condition is not null) return;
        Model.Condition = NuiConditionTree.CreateLeaf();
        RebuildConditionRoot();
    }

    /// <summary>Clears the condition — the behavior always runs.</summary>
    private void RemoveCondition()
    {
        Model.Condition = null;
        RebuildConditionRoot();
    }

    public bool HasCondition
    {
        get => Model.Condition is not null;
        set
        {
            if (value && Model.Condition is null) AddCondition();
            else if (!value && Model.Condition is not null) RemoveCondition();
            else OnPropertyChanged();
        }
    }

    // ---- action chain -------------------------------------------------------

    /// <summary>The action steps — one for the single-`action` form, the
    /// full ordered chain for `actions` (NUI-SCHEMA §7.3).</summary>
    public ObservableCollection<ActionStepViewModel> Steps { get; }

    public RelayCommand AddStepCommand { get; }
    public RelayCommand<ActionStepViewModel> RemoveStepCommand { get; }
    public RelayCommand<ActionStepViewModel> MoveStepUpCommand { get; }
    public RelayCommand<ActionStepViewModel> MoveStepDownCommand { get; }

    private void RebuildSteps()
    {
        Steps.Clear();
        var actions = Model.Actions is { Count: > 0 } chain
            ? chain
            : Model.Action is { } single ? new List<NuiAction> { single } : new();
        foreach (var action in actions)
        {
            Steps.Add(new ActionStepViewModel(action, _resolveComponentType) { Owner = this });
        }
        OnPropertyChanged(nameof(Summary));
    }

    private void AddStep()
    {
        // Migrate to the chain form the first time a second step is added
        // (the serializer omits `actions` when unused, so a lone step
        // keeps the noise-free single form).
        if (Model.Actions is not { Count: > 0 })
        {
            Model.Actions = new List<NuiAction>
            {
                Model.Action ?? new NuiAction { Target = "System" },
            };
            Model.Action = null;
        }
        Model.Actions.Add(new NuiAction { Target = "System" });
        RebuildSteps();
    }

    private void RemoveStep(ActionStepViewModel? step)
    {
        if (step is null) return;
        if (Model.Actions is { Count: > 0 } chain)
        {
            if (chain.Count <= 1) return; // a behavior needs at least one action
            chain.Remove(step.Model);
            RebuildSteps();
        }
        else if (Model.Action is not null)
        {
            // The single-action form can't drop below one step.
            return;
        }
    }

    private void MoveStepUp(ActionStepViewModel? step)
    {
        if (step is null || Model.Actions is not { Count: > 0 } chain) return;
        var index = chain.IndexOf(step.Model);
        if (index <= 0) return;
        (chain[index], chain[index - 1]) = (chain[index - 1], chain[index]);
        RebuildSteps();
    }

    private void MoveStepDown(ActionStepViewModel? step)
    {
        if (step is null || Model.Actions is not { Count: > 0 } chain) return;
        var index = chain.IndexOf(step.Model);
        if (index < 0 || index >= chain.Count - 1) return;
        (chain[index], chain[index + 1]) = (chain[index + 1], chain[index]);
        RebuildSteps();
    }

    // ---- summary ------------------------------------------------------------

    public string Summary
    {
        get
        {
            var condition = NuiConditionTree.Describe(Model.Condition);
            var conditionText = Model.Condition is null ? string.Empty : $" IF {condition}";
            var actions = Steps.Count > 0
                ? string.Join(", ", Steps.Select(s => s.Summary))
                : string.Empty;
            return $"WHEN {SourceComponent.Id}.{EventName}{conditionText} DO {actions}";
        }
    }
}
