using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Design-time wrapper around a NuiBehavior plus the component/event pair
/// it's currently bound to (a behavior is only reachable via some
/// component's Events dict — see NUI-SCHEMA.md §7). Editor-only concern,
/// kept out of Nyforge.Core per NFC-001 §5.1.
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

    public string Summary
    {
        get
        {
            var condition = Model.Condition is { } c
                ? $" IF {c.State} {(c.Operator == "equals" ? "==" : "!=")} {c.Value}"
                : string.Empty;
            var target = Model.Action.Target == "System" ? "System" : Model.Action.Target;
            return $"WHEN {SourceComponent.Id}.{EventName}{condition} DO {target}.{Model.Action.Name}";
        }
    }

    public string ActionTarget
    {
        get => Model.Action.Target;
        set
        {
            Model.Action.Target = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(AvailableActionNames));
        }
    }

    public string ActionName
    {
        get => Model.Action.Name;
        set { Model.Action.Name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    /// <summary>
    /// Valid names for the current ActionTarget, per the anti-drift rule
    /// in NUI-SCHEMA.md §7 — the editor must not let you type an action
    /// name that doesn't exist on the target's contract.
    /// </summary>
    public IReadOnlyList<string> AvailableActionNames
    {
        get
        {
            if (ActionTarget == "System")
            {
                return NuiSystemActions.All.Select(a => a.Name).ToList();
            }

            var type = _resolveComponentType(ActionTarget);
            if (type is not null && ComponentContracts.TryGet(type, out var contract))
            {
                return contract!.Actions;
            }

            return Array.Empty<string>();
        }
    }

    public bool HasCondition
    {
        get => Model.Condition is not null;
        set
        {
            Model.Condition = value ? (Model.Condition ?? new NuiCondition()) : null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Summary));
        }
    }

    public string ConditionState
    {
        get => Model.Condition?.State ?? string.Empty;
        set { if (Model.Condition is null) return; Model.Condition.State = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public string ConditionValue
    {
        get => Model.Condition?.Value?.ToString() ?? string.Empty;
        set { if (Model.Condition is null) return; Model.Condition.Value = value; OnPropertyChanged(); OnPropertyChanged(nameof(Summary)); }
    }

    public BehaviorViewModel(NuiBehavior model, NuiComponent sourceComponent, string eventName, Func<string, string?> resolveComponentType)
    {
        Model = model;
        SourceComponent = sourceComponent;
        EventName = eventName;
        _resolveComponentType = resolveComponentType;
    }
}
