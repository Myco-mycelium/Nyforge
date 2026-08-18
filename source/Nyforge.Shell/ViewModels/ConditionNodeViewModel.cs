using System.Collections.ObjectModel;
using Nyforge.Core.Nui;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// One node of the node-graph Logic Editor's condition tree (NUI-SCHEMA
/// §7.3): a leaf (expression or legacy equality) or an AND/OR group
/// holding child nodes recursively. Wraps a <see cref="NuiCondition"/> —
/// edits mutate the model in place (matching the flat editor's live-edit
/// convention); structural changes keep the model and this VM's
/// <see cref="Children"/> in lockstep. The pure tree semantics live in
/// <see cref="NuiConditionTree"/> (Nyforge.Core); this class is only the
/// Avalonia-facing presentation.
/// </summary>
public sealed class ConditionNodeViewModel : ViewModelBase
{
    public NuiCondition Model { get; }

    /// <summary>Null for the behavior's root condition; the parent group otherwise.</summary>
    public ConditionNodeViewModel? Parent { get; }

    public ConditionNodeViewModel(NuiCondition model, ConditionNodeViewModel? parent)
    {
        Model = model;
        Parent = parent;
        Children = new ObservableCollection<ConditionNodeViewModel>();
        AddLeafCommand = new RelayCommand(AddLeaf, () => IsGroup);
        AddGroupCommand = new RelayCommand(AddGroup, () => IsGroup);
        RemoveSelfCommand = new RelayCommand(RemoveSelf, () => CanRemove);
        if (NuiConditionTree.IsGroup(model) && model.Conditions is { } children)
        {
            foreach (var child in children)
            {
                Children.Add(new ConditionNodeViewModel(child, this));
            }
        }
    }

    public bool IsGroup => NuiConditionTree.IsGroup(Model);
    public bool IsLeaf => !IsGroup;

    public ObservableCollection<ConditionNodeViewModel> Children { get; }

    public IReadOnlyList<string> LogicOptions => NuiConditionTree.LogicLabels;
    public IReadOnlyList<string> OperatorOptions { get; } = new[] { "equals", "notEquals" };

    public RelayCommand AddLeafCommand { get; }
    public RelayCommand AddGroupCommand { get; }
    public RelayCommand RemoveSelfCommand { get; }

    public string? Logic
    {
        get => Model.Logic;
        set
        {
            if (!IsGroup || value is not ("and" or "or")) return;
            Model.Logic = value;
            OnPropertyChanged();
            NotifyStructuralChange();
        }
    }

    // ---- leaf editing (legacy equality form) --------------------------------

    public string State
    {
        get => Model.State;
        set { Model.State = value; OnPropertyChanged(); NotifyStructuralChange(); }
    }

    public string Operator
    {
        get => Model.Operator;
        set { Model.Operator = value; OnPropertyChanged(); NotifyStructuralChange(); }
    }

    public string Value
    {
        get => Model.Value?.ToString() ?? string.Empty;
        set { Model.Value = value; OnPropertyChanged(); NotifyStructuralChange(); }
    }

    /// <summary>Expression vs equality leaf mode (NUI-SCHEMA §7.2 supersedes
    /// the equality form when the expression is non-empty).</summary>
    public bool UsesExpression
    {
        get => !string.IsNullOrEmpty(Model.Expression);
        set
        {
            if (value)
            {
                Model.Expression ??= string.Empty;
            }
            else
            {
                Model.Expression = null;
            }
            OnPropertyChanged();
            NotifyStructuralChange();
        }
    }

    public string Expression
    {
        get => Model.Expression ?? string.Empty;
        set { Model.Expression = value; OnPropertyChanged(); NotifyStructuralChange(); }
    }

    // ---- structural commands ------------------------------------------------

    public void AddLeaf() => AddChild(NuiConditionTree.CreateLeaf());

    public void AddGroup() => AddChild(NuiConditionTree.CreateGroup("and"));

    public void AddChild(NuiCondition child)
    {
        if (!IsGroup) return;
        Model.Conditions ??= new List<NuiCondition>();
        Model.Conditions.Add(child);
        Children.Add(new ConditionNodeViewModel(child, this));
        NotifyStructuralChange();
    }

    /// <summary>Removes this node from its parent group. The root node's
    /// removal is the behavior's business (see BehaviorViewModel.RemoveCondition).</summary>
    public void RemoveSelf()
    {
        if (Parent is null) return;
        Parent.Model.Conditions?.Remove(Model);
        Parent.Children.Remove(this);
        Parent.NotifyStructuralChange();
    }

    public bool CanRemove => Parent is not null;

    public string Summary => NuiConditionTree.Describe(Model);

    /// <summary>Raised (through the tree) whenever the condition text could
    /// change, so the behavior card's summary refreshes.</summary>
    public event Action? Changed;

    private void NotifyStructuralChange()
    {
        OnPropertyChanged(nameof(Summary));
        Changed?.Invoke();
        Parent?.NotifyStructuralChange();
    }
}
