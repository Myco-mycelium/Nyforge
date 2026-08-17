using Nyforge.Core.Nui;

namespace Nyforge.Core.Editing;

/// <summary>Adds a new component under a container (or the screen root).</summary>
public sealed class AddComponentCommand : IEditorCommand
{
    private readonly NuiComponent _parent;
    private readonly NuiComponent _component;

    public AddComponentCommand(NuiComponent parent, NuiComponent component)
    {
        _parent = parent;
        _component = component;
    }

    public string Description => $"Add {_component.Type}";

    public void Execute() => _parent.Children.Add(_component);

    public void Undo() => _parent.Children.Remove(_component);
}

/// <summary>
/// Deletes a component from anywhere in the tree. Also removes the
/// behaviors that only the deleted subtree referenced (document.Behaviors
/// entries whose id appears in the subtree's Events maps), so a saved
/// .nstudio never keeps dangling references — and restores them on undo.
/// </summary>
public sealed class DeleteComponentCommand : IEditorCommand
{
    private readonly NuiDocument _document;
    private readonly NuiComponent _parent;
    private readonly int _index;
    private readonly NuiComponent _component;
    private readonly List<NuiBehavior> _orphanedBehaviors;

    public DeleteComponentCommand(NuiDocument document, NuiComponent parent, int index, NuiComponent component)
    {
        _document = document;
        _parent = parent;
        _index = index;
        _component = component;

        // Behaviors that only this subtree references (their id appears in
        // the subtree's Events maps) must leave document.Behaviors with the
        // node, or the saved .nstudio keeps dangling entries — and they must
        // come back on undo.
        var referencedIds = ComponentTree.Walk(component)
            .SelectMany(n => n.Events.Values)
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal);
        _orphanedBehaviors = document.Behaviors
            .Where(b => referencedIds.Contains(b.Id))
            .ToList();
    }

    public string Description => $"Delete {_component.Type} '{_component.Id}'";

    public void Execute()
    {
        if (_index < _parent.Children.Count && _parent.Children[_index] == _component)
        {
            _parent.Children.RemoveAt(_index);
        }
        else
        {
            _parent.Children.Remove(_component);
        }
        foreach (var behavior in _orphanedBehaviors)
        {
            // The Events entries travel with the deleted nodes; the only
            // dangling reference is the behavior object in Behaviors[].
            _document.Behaviors.Remove(behavior);
        }
    }

    public void Undo()
    {
        _parent.Children.Insert(Math.Min(_index, _parent.Children.Count), _component);
        foreach (var behavior in _orphanedBehaviors)
        {
            _document.Behaviors.Add(behavior);
        }
    }
}

/// <summary>Moves a component within its parent (relative Layout offsets).</summary>
public sealed class MoveComponentCommand : IEditorCommand
{
    private readonly NuiComponent _component;
    private readonly double _oldX, _oldY, _newX, _newY;

    public MoveComponentCommand(NuiComponent component, double oldX, double oldY, double newX, double newY)
    {
        _component = component;
        _oldX = oldX;
        _oldY = oldY;
        _newX = newX;
        _newY = newY;
    }

    public string Description => $"Move {_component.Type} '{_component.Id}'";

    public void Execute()
    {
        _component.Layout.X = _newX;
        _component.Layout.Y = _newY;
    }

    public void Undo()
    {
        _component.Layout.X = _oldX;
        _component.Layout.Y = _oldY;
    }
}

/// <summary>Resizes a component.</summary>
public sealed class ResizeComponentCommand : IEditorCommand
{
    private readonly NuiComponent _component;
    private readonly double _oldWidth, _oldHeight, _newWidth, _newHeight;

    public ResizeComponentCommand(NuiComponent component, double oldWidth, double oldHeight, double newWidth, double newHeight)
    {
        _component = component;
        _oldWidth = oldWidth;
        _oldHeight = oldHeight;
        _newWidth = newWidth;
        _newHeight = newHeight;
    }

    public string Description => $"Resize {_component.Type} '{_component.Id}'";

    public void Execute()
    {
        _component.Layout.Width = _newWidth;
        _component.Layout.Height = _newHeight;
    }

    public void Undo()
    {
        _component.Layout.Width = _oldWidth;
        _component.Layout.Height = _oldHeight;
    }
}

/// <summary>Changes one property value, restoring the exact prior state (or absence) on undo.</summary>
public sealed class ChangePropertyCommand : IEditorCommand
{
    private readonly NuiComponent _component;
    private readonly string _property;
    private readonly object? _oldValue;
    private readonly object? _newValue;
    private readonly bool _existed;

    public ChangePropertyCommand(NuiComponent component, string property, object? oldValue, object? newValue)
    {
        _component = component;
        _property = property;
        _oldValue = oldValue;
        _newValue = newValue;
        _existed = component.Properties.ContainsKey(property);
    }

    public string Description => $"Change {_component.Type} '{_component.Id}'.{_property}";

    public void Execute() => _component.Properties[_property] = _newValue;

    public void Undo()
    {
        if (_existed) _component.Properties[_property] = _oldValue;
        else _component.Properties.Remove(_property);
    }
}

/// <summary>
/// Reorders a component within its parent's Children list (z-order): the
/// component ends up immediately before the sibling at the original
/// <paramref name="targetIndex"/>. Undo restores the exact old index.
/// </summary>
public sealed class ReorderComponentCommand : IEditorCommand
{
    private readonly NuiComponent _parent;
    private readonly NuiComponent _component;
    private readonly int _oldIndex;
    private readonly int _insertIndex;

    public ReorderComponentCommand(NuiComponent parent, NuiComponent component, int oldIndex, int targetIndex)
    {
        _parent = parent;
        _component = component;
        _oldIndex = oldIndex;
        // Insert before the target sibling; after removal the target's
        // index shifts down by one when it sat after the component.
        _insertIndex = targetIndex > oldIndex ? targetIndex - 1 : targetIndex;
    }

    public string Description => $"Reorder {_component.Type} '{_component.Id}'";

    public void Execute()
    {
        _parent.Children.Remove(_component);
        _parent.Children.Insert(Math.Min(_insertIndex, _parent.Children.Count), _component);
    }

    public void Undo()
    {
        _parent.Children.Remove(_component);
        _parent.Children.Insert(Math.Min(_oldIndex, _parent.Children.Count), _component);
    }
}

/// <summary>
/// Moves a component to a new parent, preserving its absolute canvas
/// position (ComponentTree.Reparent) and restoring the exact old parent
/// and z-order on undo.
/// </summary>
public sealed class ReparentComponentCommand : IEditorCommand
{
    private readonly NuiComponent _root;
    private readonly NuiComponent _component;
    private readonly NuiComponent _oldParent;
    private readonly int _oldIndex;
    private readonly NuiComponent _newParent;

    public ReparentComponentCommand(
        NuiComponent root, NuiComponent component,
        NuiComponent oldParent, int oldIndex, NuiComponent newParent)
    {
        _root = root;
        _component = component;
        _oldParent = oldParent;
        _oldIndex = oldIndex;
        _newParent = newParent;
    }

    public string Description => $"Reparent {_component.Type} '{_component.Id}'";

    public void Execute() => ComponentTree.Reparent(_root, _component, _newParent);

    public void Undo() => ComponentTree.Reparent(_root, _component, _oldParent, _oldIndex);
}

/// <summary>Adds a behavior and binds it to a component's event.</summary>
public sealed class AddBehaviorCommand : IEditorCommand
{
    private readonly NuiDocument _document;
    private readonly NuiComponent _component;
    private readonly string _eventName;
    private readonly NuiBehavior _behavior;

    public AddBehaviorCommand(NuiDocument document, NuiComponent component, string eventName, NuiBehavior behavior)
    {
        _document = document;
        _component = component;
        _eventName = eventName;
        _behavior = behavior;
    }

    public string Description => $"Add behavior '{_behavior.Id}' to {_component.Id}.{_eventName}";

    public void Execute()
    {
        _document.Behaviors.Add(_behavior);
        _component.Events[_eventName] = _behavior.Id;
    }

    public void Undo()
    {
        _document.Behaviors.Remove(_behavior);
        _component.Events[_eventName] = null;
    }
}

/// <summary>Removes a behavior and unbinds its event.</summary>
public sealed class DeleteBehaviorCommand : IEditorCommand
{
    private readonly NuiDocument _document;
    private readonly NuiComponent _component;
    private readonly string _eventName;
    private readonly NuiBehavior _behavior;

    public DeleteBehaviorCommand(NuiDocument document, NuiComponent component, string eventName, NuiBehavior behavior)
    {
        _document = document;
        _component = component;
        _eventName = eventName;
        _behavior = behavior;
    }

    public string Description => $"Delete behavior '{_behavior.Id}' from {_component.Id}.{_eventName}";

    public void Execute()
    {
        _document.Behaviors.Remove(_behavior);
        _component.Events[_eventName] = null;
    }

    public void Undo()
    {
        _document.Behaviors.Add(_behavior);
        _component.Events[_eventName] = _behavior.Id;
    }
}
