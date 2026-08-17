using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Copy/paste payload round-tripping and fresh-id cloning (v0.6 Priority 1
/// copy/paste). Ids must stay document-unique after paste, and event
/// wiring must not cross the clipboard since behaviors are document-scoped.
/// </summary>
public class ComponentClipboardTests
{
    private static NuiComponent NestedTree()
    {
        var container = new NuiComponent
        {
            Id = "container_1",
            Type = "Container",
            Layout = new NuiLayout { X = 240, Y = 24, Width = 740, Height = 700 }
        };
        container.Children.Add(new NuiComponent
        {
            Id = "toggle_1",
            Type = "Toggle",
            Layout = new NuiLayout { X = 0, Y = 40, Width = 280, Height = 32 },
            Properties = new Dictionary<string, object?> { ["value"] = true, ["label"] = "Eclipse" }
        });
        container.Children.Add(new NuiComponent
        {
            Id = "button_1",
            Type = "Button",
            Layout = new NuiLayout { X = 0, Y = 320, Width = 120, Height = 36 },
            Events = new Dictionary<string, string?> { ["clicked"] = "behavior_1" }
        });
        return container;
    }

    [Fact]
    public void Serialize_then_deserialize_preserves_tree_and_values()
    {
        var source = NestedTree();

        var payload = ComponentClipboard.Serialize(new[] { source });
        var restored = ComponentClipboard.Deserialize(payload);

        var container = Assert.Single(restored);
        Assert.Equal("Container", container.Type);
        Assert.Equal((240.0, 24.0), (container.Layout.X, container.Layout.Y));

        var toggle = Assert.Single(container.Children.Where(c => c.Type == "Toggle"));
        Assert.True((bool)toggle.Properties["value"]!); // native bool, not JsonElement
        Assert.Equal("Eclipse", toggle.Properties["label"]);
        Assert.Equal((0.0, 40.0), (toggle.Layout.X, toggle.Layout.Y));
    }

    [Fact]
    public void Garbage_payload_deserializes_to_empty()
    {
        Assert.Empty(ComponentClipboard.Deserialize("this is not json"));
        Assert.Empty(ComponentClipboard.Deserialize(""));
    }

    [Fact]
    public void CloneWithFreshIds_assigns_unique_ids_and_strips_events()
    {
        var source = NestedTree();
        var clone = Assert.Single(ComponentClipboard.CloneWithFreshIds(new[] { source }));

        Assert.NotEqual(source.Id, clone.Id);
        Assert.NotEqual(source.Children[0].Id, clone.Children[0].Id);
        Assert.NotEqual(source.Children[1].Id, clone.Children[1].Id);

        // All ids distinct from the source's.
        var cloneIds = ComponentClipboard.CloneWithFreshIds(new[] { source })
            .SelectMany(c => ComponentTree.Walk(c))
            .Select(c => c.Id)
            .ToHashSet();
        Assert.DoesNotContain(source.Id, cloneIds);
        Assert.DoesNotContain(source.Children[0].Id, cloneIds);

        // Structure + layout preserved.
        Assert.Equal(2, clone.Children.Count);
        Assert.Equal((0.0, 320.0), (clone.Children[1].Layout.X, clone.Children[1].Layout.Y));

        // Events stripped — pasted components arrive unbound.
        Assert.Empty(clone.Children[1].Events);
    }

    [Fact]
    public void CloneWithFreshIds_of_multiple_components_keeps_them_distinct()
    {
        var a = new NuiComponent { Id = "text_1", Type = "Text", Layout = new NuiLayout { X = 40, Y = 40 } };
        var b = new NuiComponent { Id = "text_2", Type = "Text", Layout = new NuiLayout { X = 200, Y = 40 } };

        var clones = ComponentClipboard.CloneWithFreshIds(new[] { a, b });

        Assert.Equal(2, clones.Count);
        Assert.NotEqual(clones[0].Id, clones[1].Id);
        Assert.Equal((40.0, 40.0), (clones[0].Layout.X, clones[0].Layout.Y));
        Assert.Equal((200.0, 40.0), (clones[1].Layout.X, clones[1].Layout.Y));
    }

    [Fact]
    public void Serialized_payload_round_trips_through_clone()
    {
        var source = NestedTree();
        var payload = ComponentClipboard.Serialize(new[] { source });
        var clones = ComponentClipboard.CloneWithFreshIds(ComponentClipboard.Deserialize(payload));

        var container = Assert.Single(clones);
        Assert.Equal("Container", container.Type);
        Assert.Equal(2, container.Children.Count);
        Assert.NotEqual(source.Id, container.Id);
    }
}
