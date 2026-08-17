using Nyforge.Core.Editing;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Drag-to-reorder within a parent (z-order change): dropping a component
/// onto a sibling places it immediately before that sibling, and undo
/// restores the exact original index.
/// </summary>
public class ReorderCommandTests
{
    private static NuiComponent ParentWith(int count)
    {
        var parent = new NuiComponent { Id = "container_1", Type = "Container" };
        for (var i = 0; i < count; i++)
        {
            parent.Children.Add(new NuiComponent { Id = $"item_{i}", Type = "Text" });
        }
        return parent;
    }

    [Fact]
    public void Reorder_moves_element_before_a_later_sibling()
    {
        var parent = ParentWith(4); // item_0, item_1, item_2, item_3
        var history = new CommandHistory();

        // Move item_0 before item_3 (original index 3, after the element).
        history.Execute(new ReorderComponentCommand(parent, parent.Children[0], 0, 3));

        Assert.Equal(new[] { "item_1", "item_2", "item_0", "item_3" },
            parent.Children.Select(c => c.Id));
    }

    [Fact]
    public void Reorder_moves_element_before_an_earlier_sibling()
    {
        var parent = ParentWith(4);
        var history = new CommandHistory();

        // Move item_3 before item_1 (original index 1, before the element).
        history.Execute(new ReorderComponentCommand(parent, parent.Children[3], 3, 1));

        Assert.Equal(new[] { "item_0", "item_3", "item_1", "item_2" },
            parent.Children.Select(c => c.Id));
    }

    [Fact]
    public void Undo_restores_the_exact_original_order()
    {
        var parent = ParentWith(4);
        var history = new CommandHistory();

        history.Execute(new ReorderComponentCommand(parent, parent.Children[3], 3, 1));
        history.Undo();

        Assert.Equal(new[] { "item_0", "item_1", "item_2", "item_3" },
            parent.Children.Select(c => c.Id));

        history.Redo();
        Assert.Equal(new[] { "item_0", "item_3", "item_1", "item_2" },
            parent.Children.Select(c => c.Id));
    }

    [Fact]
    public void Reorder_to_adjacent_sibling_swaps_correctly()
    {
        var parent = ParentWith(3); // item_0, item_1, item_2
        var history = new CommandHistory();

        // item_1 before item_2 -> item_0, item_1, item_2 is unchanged? No:
        // before item_2 means item_1 stays (it's already before item_2).
        history.Execute(new ReorderComponentCommand(parent, parent.Children[1], 1, 2));
        Assert.Equal(new[] { "item_0", "item_1", "item_2" }, parent.Children.Select(c => c.Id));

        // item_2 before item_0 -> item_2, item_0, item_1
        history.Execute(new ReorderComponentCommand(parent, parent.Children[2], 2, 0));
        Assert.Equal(new[] { "item_2", "item_0", "item_1" }, parent.Children.Select(c => c.Id));
    }

    [Fact]
    public void Reorder_round_trips_through_serialization()
    {
        var doc = NyforgeProject.CreateBlank();
        var root = doc.Screens[0].Root;
        root.Children.Add(ParentWith(3));

        var parent = root.Children[0];
        var history = new CommandHistory();
        history.Execute(new ReorderComponentCommand(parent, parent.Children[2], 2, 0));

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);
        var reloadedParent = ComponentTree.Find(reloaded.Screens[0].Root, "container_1")!;

        Assert.Equal(new[] { "item_2", "item_0", "item_1" },
            reloadedParent.Children.Select(c => c.Id));
    }
}
