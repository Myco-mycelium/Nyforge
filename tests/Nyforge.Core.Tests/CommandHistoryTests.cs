using Nyforge.Core.Editing;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Command-based undo/redo (2026-08-17 architecture review, item #5): one
/// IEditorCommand per completed gesture, undo/redo stacks, no snapshots.
/// These tests pin the command semantics and the delete/orphaned-behavior
/// and reparent/z-order restore paths.
/// </summary>
public class CommandHistoryTests
{
    private static NuiDocument BlankDoc()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "container_1", Type = "Container",
            Layout = new NuiLayout { X = 40, Y = 40, Width = 320, Height = 220 }
        });
        return doc;
    }

    private static NuiComponent Leaf(string id, string type, double x, double y) =>
        new() { Id = id, Type = type, Layout = new NuiLayout { X = x, Y = y, Width = 100, Height = 32 } };

    [Fact]
    public void Undo_then_redo_restores_the_exact_state()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var leaf = Leaf("leaf_1", "Text", 0, 0);
        var history = new CommandHistory();

        history.Execute(new AddComponentCommand(container, leaf));
        Assert.Contains(leaf, container.Children);

        history.Undo();
        Assert.DoesNotContain(leaf, container.Children);

        history.Redo();
        Assert.Contains(leaf, container.Children);
    }

    [Fact]
    public void New_execute_clears_the_redo_stack()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var a = Leaf("a", "Text", 0, 0);
        var b = Leaf("b", "Text", 0, 0);
        var history = new CommandHistory();

        history.Execute(new AddComponentCommand(container, a));
        history.Undo();
        Assert.True(history.CanRedo);

        history.Execute(new AddComponentCommand(container, b));
        Assert.False(history.CanRedo); // the undone 'a' is no longer redoable
        Assert.DoesNotContain(a, container.Children);
        Assert.Contains(b, container.Children);
    }

    [Fact]
    public void Undo_and_redo_on_empty_history_are_noops()
    {
        var history = new CommandHistory();
        history.Undo();
        history.Redo();
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Limit_trims_the_oldest_commands()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var history = new CommandHistory { Limit = 2 };

        history.Execute(new AddComponentCommand(container, Leaf("one", "Text", 0, 0)));
        history.Execute(new AddComponentCommand(container, Leaf("two", "Text", 0, 0)));
        history.Execute(new AddComponentCommand(container, Leaf("three", "Text", 0, 0)));

        Assert.True(history.CanUndo);
        history.Undo(); // undoes 'three'
        history.Undo(); // undoes 'two' — 'one' was trimmed
        Assert.False(history.CanUndo);
        Assert.Single(container.Children); // only 'one' remains
    }

    [Fact]
    public void DeleteComponent_removes_orphaned_behaviors_and_undo_restores_them()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var leaf = Leaf("leaf_1", "Button", 0, 0);
        container.Children.Add(leaf);

        var behavior = new NuiBehavior
        {
            Id = "behavior_1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" }
        };
        doc.Behaviors.Add(behavior);
        leaf.Events["clicked"] = behavior.Id;

        var (parent, index) = ComponentTree.FindParentAndIndex(doc.Screens[0].Root, "leaf_1");
        var history = new CommandHistory();

        history.Execute(new DeleteComponentCommand(doc, parent!, index, leaf));

        Assert.DoesNotContain(leaf, container.Children);
        Assert.DoesNotContain(behavior, doc.Behaviors); // no dangling reference

        history.Undo();

        Assert.Contains(leaf, container.Children);
        Assert.Contains(behavior, doc.Behaviors);
        Assert.Equal(behavior.Id, leaf.Events["clicked"]);
    }

    [Fact]
    public void DeleteComponent_preserves_non_orphaned_behaviors()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var leaf = Leaf("leaf_1", "Button", 0, 0);
        container.Children.Add(leaf);

        var kept = new NuiBehavior { Id = "behavior_keep", Action = new NuiAction { Name = "X" } };
        var orphaned = new NuiBehavior { Id = "behavior_orphan", Action = new NuiAction { Name = "Y" } };
        doc.Behaviors.Add(kept);
        doc.Behaviors.Add(orphaned);
        leaf.Events["clicked"] = orphaned.Id;
        // 'kept' is referenced from elsewhere (another component), so deleting
        // leaf must NOT remove it.
        doc.Screens[0].Root.Children.Add(Leaf("other", "Text", 300, 0));
        doc.Screens[0].Root.Children[1].Events["tapped"] = kept.Id;

        var (parent, index) = ComponentTree.FindParentAndIndex(doc.Screens[0].Root, "leaf_1");
        var history = new CommandHistory();
        history.Execute(new DeleteComponentCommand(doc, parent!, index, leaf));

        Assert.DoesNotContain(orphaned, doc.Behaviors);
        Assert.Contains(kept, doc.Behaviors);
    }

    [Fact]
    public void Move_and_resize_commands_restore_exact_values()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var history = new CommandHistory();

        history.Execute(new MoveComponentCommand(container, 40, 40, 120, 90));
        Assert.Equal((120.0, 90.0), (container.Layout.X, container.Layout.Y));
        history.Undo();
        Assert.Equal((40.0, 40.0), (container.Layout.X, container.Layout.Y));
        history.Redo();
        Assert.Equal((120.0, 90.0), (container.Layout.X, container.Layout.Y));

        history.Execute(new ResizeComponentCommand(container, 320, 220, 500, 400));
        Assert.Equal((500.0, 400.0), (container.Layout.Width, container.Layout.Height));
        history.Undo();
        Assert.Equal((320.0, 220.0), (container.Layout.Width, container.Layout.Height));
    }

    [Fact]
    public void ChangeProperty_undo_restores_absence_when_property_did_not_exist()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var history = new CommandHistory();

        Assert.False(container.Properties.ContainsKey("background"));
        history.Execute(new ChangePropertyCommand(container, "background", null, "#123456"));
        Assert.Equal("#123456", container.Properties["background"]);

        history.Undo();
        Assert.False(container.Properties.ContainsKey("background")); // fully removed, not nulled
    }

    [Fact]
    public void Reparent_command_undo_restores_old_parent_and_z_order()
    {
        var doc = BlankDoc();
        var root = doc.Screens[0].Root;
        var containerA = root.Children[0]; // at (40, 40)
        var containerB = Leaf("container_b", "Container", 400, 60);
        root.Children.Add(containerB);
        var leaf = Leaf("leaf_1", "Toggle", 0, 40);
        containerA.Children.Add(leaf); // abs (40, 80)

        var history = new CommandHistory();
        history.Execute(new ReparentComponentCommand(root, leaf, containerA, 0, containerB));

        Assert.Contains(leaf, containerB.Children);
        Assert.DoesNotContain(leaf, containerA.Children);
        // leaf abs was (40, 80); containerB abs is (400, 60) -> relative (40-400, 80-60)
        Assert.Equal((-360.0, 20.0), (leaf.Layout.X, leaf.Layout.Y));
        Assert.Equal((40.0, 80.0), ComponentTree.AbsolutePosition(root, leaf));

        history.Undo();

        Assert.Contains(leaf, containerA.Children);
        Assert.DoesNotContain(leaf, containerB.Children);
        Assert.Equal(0, containerA.Children.IndexOf(leaf)); // exact z-order restored
        Assert.Equal((0.0, 40.0), (leaf.Layout.X, leaf.Layout.Y));
        Assert.Equal((40.0, 80.0), ComponentTree.AbsolutePosition(root, leaf));
    }

    [Fact]
    public void Behavior_add_and_delete_commands_round_trip()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var behavior = new NuiBehavior { Id = "behavior_1", Action = new NuiAction { Name = "Nyrqis.Theme.Set" } };
        var history = new CommandHistory();

        history.Execute(new AddBehaviorCommand(doc, container, "clicked", behavior));
        Assert.Contains(behavior, doc.Behaviors);
        Assert.Equal(behavior.Id, container.Events["clicked"]);

        history.Execute(new DeleteBehaviorCommand(doc, container, "clicked", behavior));
        Assert.DoesNotContain(behavior, doc.Behaviors);
        Assert.Null(container.Events["clicked"]);

        history.Undo(); // behavior back
        Assert.Contains(behavior, doc.Behaviors);
        Assert.Equal(behavior.Id, container.Events["clicked"]);

        history.Undo(); // add undone
        Assert.DoesNotContain(behavior, doc.Behaviors);
        Assert.Null(container.Events["clicked"]);
    }

    [Fact]
    public void Changed_event_fires_on_execute_undo_and_redo()
    {
        var doc = BlankDoc();
        var container = doc.Screens[0].Root.Children[0];
        var history = new CommandHistory();
        var count = 0;
        history.Changed += (_, _) => count++;

        history.Execute(new AddComponentCommand(container, Leaf("l", "Text", 0, 0)));
        history.Undo();
        history.Redo();

        Assert.Equal(3, count);
    }
}
