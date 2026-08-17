using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Tests for ComponentTree — the pure tree operations behind nested-tree
/// canvas editing (v0.6, Priority 1 of the 2026-08-17 architecture
/// review). Layout coordinates are relative to the parent; these tests pin
/// the position-preserving reparent math and the anti-cycle rules.
/// </summary>
public class ComponentTreeTests
{
    private static NuiComponent Container(string id, double x, double y, double w = 320, double h = 220) =>
        new() { Id = id, Type = "Container", Layout = new NuiLayout { X = x, Y = y, Width = w, Height = h } };

    private static NuiComponent Leaf(string id, string type, double x, double y) =>
        new() { Id = id, Type = type, Layout = new NuiLayout { X = x, Y = y, Width = 100, Height = 32 } };

    /// <summary>
    /// Mirrors examples/settings-app/settings-app.nstudio's structure:
    /// Window > [Sidebar > [Link, Link], Container > [Text, Toggle, Button]].
    /// </summary>
    private static NuiComponent SettingsAppTree()
    {
        var window = new NuiComponent { Id = "window_settings", Type = "Window", Layout = new NuiLayout { Width = 1024, Height = 768 } };

        var sidebar = Container("sidebar_nav", 0, 0, 220, 768);
        sidebar.Children.Add(Leaf("nav_appearance", "Link", 16, 24));
        sidebar.Children.Add(Leaf("nav_general", "Link", 16, 64));

        var page = Container("page_appearance", 240, 24, 740, 700);
        page.Children.Add(Leaf("label_theme", "Text", 0, 0));
        page.Children.Add(Leaf("toggle_eclipse", "Toggle", 0, 40));
        page.Children.Add(Leaf("button_save", "Button", 0, 320));

        window.Children.Add(sidebar);
        window.Children.Add(page);
        return window;
    }

    [Fact]
    public void Find_locates_nested_nodes_by_id()
    {
        var root = SettingsAppTree();

        Assert.NotNull(ComponentTree.Find(root, "toggle_eclipse"));
        Assert.NotNull(ComponentTree.Find(root, "sidebar_nav"));
        Assert.Null(ComponentTree.Find(root, "does_not_exist"));
    }

    [Fact]
    public void FindParentAndIndex_reports_the_direct_parent()
    {
        var root = SettingsAppTree();

        var (parent, index) = ComponentTree.FindParentAndIndex(root, "toggle_eclipse");
        Assert.NotNull(parent);
        Assert.Equal("page_appearance", parent!.Id);
        Assert.Equal(1, index);

        var (noParent, noIndex) = ComponentTree.FindParentAndIndex(root, "window_settings");
        Assert.Null(noParent);
        Assert.Equal(-1, noIndex);
    }

    [Fact]
    public void Remove_deletes_from_anywhere_in_the_tree()
    {
        var root = SettingsAppTree();

        var removed = ComponentTree.Remove(root, "nav_general");
        Assert.NotNull(removed);
        Assert.Equal("nav_general", removed!.Id);
        Assert.Null(ComponentTree.Find(root, "nav_general"));
        Assert.Single(ComponentTree.Find(root, "sidebar_nav")!.Children);

        Assert.Null(ComponentTree.Remove(root, "nav_general")); // already gone
    }

    [Fact]
    public void Insert_refuses_non_container_parents_without_mutating()
    {
        var root = SettingsAppTree();
        var button = ComponentTree.Find(root, "button_save")!;
        var child = Leaf("sneaky", "Text", 0, 0);

        var ok = ComponentTree.Insert(button, child);

        Assert.False(ok);
        Assert.Empty(button.Children);
        Assert.Null(ComponentTree.Find(root, "sneaky"));
    }

    [Fact]
    public void Walk_is_depth_first_parents_before_children()
    {
        var root = SettingsAppTree();

        var ids = ComponentTree.Walk(root).Select(n => n.Id).ToList();

        Assert.Equal(
            new[]
            {
                "window_settings",
                "sidebar_nav", "nav_appearance", "nav_general",
                "page_appearance", "label_theme", "toggle_eclipse", "button_save"
            },
            ids);
    }

    [Fact]
    public void Count_covers_the_whole_subtree()
    {
        var root = SettingsAppTree();
        Assert.Equal(8, ComponentTree.Count(root));
    }

    [Fact]
    public void AbsolutePosition_sums_relative_offsets_up_the_chain()
    {
        var root = SettingsAppTree();

        Assert.Equal((0.0, 0.0), ComponentTree.AbsolutePosition(root, root));
        Assert.Equal((240.0, 24.0), ComponentTree.AbsolutePosition(root, ComponentTree.Find(root, "page_appearance")!));
        // Toggle is at (0, 40) inside Container at (240, 24) -> (240, 64)
        Assert.Equal((240.0, 64.0), ComponentTree.AbsolutePosition(root, ComponentTree.Find(root, "toggle_eclipse")!));
    }

    [Fact]
    public void Reparent_moves_a_node_across_parents_preserving_absolute_position()
    {
        var root = SettingsAppTree();
        var toggle = ComponentTree.Find(root, "toggle_eclipse")!;
        var sidebar = ComponentTree.Find(root, "sidebar_nav")!;

        var ok = ComponentTree.Reparent(root, toggle, sidebar);

        Assert.True(ok);
        Assert.Same(toggle, ComponentTree.Find(root, "toggle_eclipse"));
        Assert.Contains(toggle, sidebar.Children);
        Assert.DoesNotContain(toggle, ComponentTree.Find(root, "page_appearance")!.Children);

        // Toggle was at absolute (240, 64); Sidebar sits at (0, 0), so the
        // relative Layout must now be (240, 64) — same visual position.
        Assert.Equal((240.0, 64.0), (toggle.Layout.X, toggle.Layout.Y));
        Assert.Equal((240.0, 64.0), ComponentTree.AbsolutePosition(root, toggle));
    }

    [Fact]
    public void Reparent_into_nested_parent_adjusts_relative_layout_correctly()
    {
        var root = SettingsAppTree();
        // Nest a Container inside the sidebar so the target parent is not at (0,0).
        var inner = Container("inner_box", 10, 20, 100, 100);
        ComponentTree.Find(root, "sidebar_nav")!.Children.Add(inner);

        var toggle = ComponentTree.Find(root, "toggle_eclipse")!; // abs (240, 64)
        var ok = ComponentTree.Reparent(root, toggle, inner);

        Assert.True(ok);
        // inner is at abs (10, 20) -> new relative offsets: (240-10, 64-20)
        Assert.Equal((230.0, 44.0), (toggle.Layout.X, toggle.Layout.Y));
        Assert.Equal((240.0, 64.0), ComponentTree.AbsolutePosition(root, toggle));
    }

    [Fact]
    public void Reparent_rejects_self_non_container_and_cycle_cases()
    {
        var root = SettingsAppTree();
        var page = ComponentTree.Find(root, "page_appearance")!;
        var toggle = ComponentTree.Find(root, "toggle_eclipse")!;
        var button = ComponentTree.Find(root, "button_save")!;

        Assert.False(ComponentTree.Reparent(root, toggle, toggle));           // into itself
        Assert.False(ComponentTree.Reparent(root, toggle, button));           // non-container target
        Assert.False(ComponentTree.Reparent(root, page, toggle));             // target inside its own subtree
        Assert.Same(toggle, ComponentTree.Find(root, "toggle_eclipse"));      // nothing moved
        Assert.Same(page, ComponentTree.Find(root, "page_appearance"));
    }

    [Fact]
    public void Reparent_then_serialize_round_trips_positions()
    {
        var doc = NyforgeProject.CreateBlank();
        var root = doc.Screens[0].Root;

        var sidebar = Container("sidebar_nav", 0, 0, 220, 768);
        var page = Container("page_appearance", 240, 24, 740, 700);
        var toggle = Leaf("toggle_eclipse", "Toggle", 0, 40);
        page.Children.Add(toggle);
        root.Children.Add(sidebar);
        root.Children.Add(page);

        Assert.True(ComponentTree.Reparent(root, toggle, sidebar));

        var json = ProjectSerializer.Serialize(doc);
        var reloaded = ProjectSerializer.Deserialize(json);
        var reloadedRoot = reloaded.Screens[0].Root;

        var reloadedToggle = ComponentTree.Find(reloadedRoot, "toggle_eclipse")!;
        var reloadedSidebar = ComponentTree.Find(reloadedRoot, "sidebar_nav")!;
        Assert.Contains(reloadedToggle, reloadedSidebar.Children);
        Assert.Equal((240.0, 64.0), (reloadedToggle.Layout.X, reloadedToggle.Layout.Y));
        Assert.Equal((240.0, 64.0), ComponentTree.AbsolutePosition(reloadedRoot, reloadedToggle));
    }

    [Fact]
    public void Container_types_match_the_vocabulary()
    {
        Assert.True(ComponentTree.CanContainChildren("Container"));
        Assert.True(ComponentTree.CanContainChildren("Stack"));
        Assert.True(ComponentTree.CanContainChildren("Grid"));
        Assert.True(ComponentTree.CanContainChildren("Window"));
        Assert.False(ComponentTree.CanContainChildren("Button"));
        Assert.False(ComponentTree.CanContainChildren("Text"));
        Assert.False(ComponentTree.CanContainChildren("Toggle"));
    }
}
