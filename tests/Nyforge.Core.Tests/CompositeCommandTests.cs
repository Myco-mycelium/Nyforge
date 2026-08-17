using Nyforge.Core.Editing;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// CompositeCommand groups several commands into one undo step — the unit
/// behind multi-select gestures (move N components, delete N components).
/// Undo runs in reverse so nested selections restore cleanly.
/// </summary>
public class CompositeCommandTests
{
    [Fact]
    public void Execute_runs_in_order_and_undo_reverses_it()
    {
        var doc = NyforgeProject.CreateBlank();
        var container = doc.Screens[0].Root;
        var a = new NuiComponent { Id = "a", Type = "Text" };
        var b = new NuiComponent { Id = "b", Type = "Text" };

        var history = new CommandHistory();
        history.Execute(new CompositeCommand(new IEditorCommand[]
        {
            new AddComponentCommand(container, a),
            new AddComponentCommand(container, b)
        }));

        Assert.Equal(new[] { "a", "b" }, container.Children.Select(c => c.Id));

        history.Undo();
        Assert.Empty(container.Children);

        history.Redo();
        Assert.Equal(new[] { "a", "b" }, container.Children.Select(c => c.Id));
    }

    [Fact]
    public void Description_joins_single_and_counts_many()
    {
        var doc = NyforgeProject.CreateBlank();
        var container = doc.Screens[0].Root;

        var single = new CompositeCommand(new IEditorCommand[]
        {
            new MoveComponentCommand(container, 0, 0, 4, 4)
        });
        Assert.Equal("Move Window 'window_main'", single.Description);

        var many = new CompositeCommand(new IEditorCommand[]
        {
            new MoveComponentCommand(container, 0, 0, 4, 4),
            new MoveComponentCommand(container, 0, 0, 8, 8)
        });
        Assert.Equal("2 operations", many.Description);
    }

    [Fact]
    public void Multi_delete_of_a_container_and_its_child_undoes_cleanly()
    {
        var doc = NyforgeProject.CreateBlank();
        var root = doc.Screens[0].Root;
        var container = new NuiComponent { Id = "container_1", Type = "Container", Layout = new NuiLayout { Width = 320, Height = 220 } };
        var child = new NuiComponent { Id = "child_1", Type = "Text" };
        container.Children.Add(child);
        root.Children.Add(container);

        var history = new CommandHistory();
        history.Execute(new CompositeCommand(new IEditorCommand[]
        {
            new DeleteComponentCommand(doc, root, 0, container),
            new DeleteComponentCommand(doc, container, 0, child)
        }));

        Assert.Empty(root.Children);
        Assert.Empty(container.Children);

        history.Undo();

        // Child re-inserted into the (detached) container first, then the
        // container re-attached — order and nesting both restored.
        Assert.Contains(container, root.Children);
        Assert.Equal(0, root.Children.IndexOf(container));
        Assert.Contains(child, container.Children);
        Assert.Equal(0, container.Children.IndexOf(child));
    }
}
