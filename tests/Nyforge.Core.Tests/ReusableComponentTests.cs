using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Reusable components (NFS-006 §9): a node with componentRef + overrides
/// resolves against the document's components[] masters instead of
/// carrying a copied tree.
/// </summary>
public class ReusableComponentTests
{
    private static NuiDocument DocumentWithTaskbarButtonMaster()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.ReusableComponents.Add(new NuiComponent
        {
            Id = "TaskbarButton",
            Type = "Button",
            Properties = new Dictionary<string, object?>
            {
                ["text"] = "App",
                ["icon"] = "AppIcon",
                ["enabled"] = true,
            },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        });
        return doc;
    }

    [Fact]
    public void Instance_without_ref_is_not_an_instance()
    {
        var node = new NuiComponent { Id = "plain", Type = "Button" };
        Assert.False(ReusableComponentResolver.IsInstance(node));
        Assert.Null(ReusableComponentResolver.Resolve(node, DocumentWithTaskbarButtonMaster()));
    }

    [Fact]
    public void Instance_resolves_master_tree()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        var instance = new NuiComponent
        {
            Id = "taskbar_files",
            ComponentRef = "TaskbarButton",
            Layout = new NuiLayout { X = 216, Y = 16, Width = 96, Height = 48 },
        };

        var resolved = ReusableComponentResolver.Resolve(instance, doc);
        Assert.NotNull(resolved);
        Assert.Equal("taskbar_files", resolved!.Id);
        Assert.Equal("Button", resolved.Type);
        Assert.Equal("App", resolved.Properties["text"]);
        Assert.Equal(96d, resolved.Layout.Width);
        // Instance placement wins.
        Assert.Equal(216d, resolved.Layout.X);
    }

    [Fact]
    public void Overrides_win_over_master_properties()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        var instance = new NuiComponent
        {
            Id = "taskbar_vault",
            ComponentRef = "TaskbarButton",
            Overrides = new Dictionary<string, object?> { ["text"] = "Vault", ["icon"] = "VaultIcon" },
        };

        var resolved = ReusableComponentResolver.Resolve(instance, doc);
        Assert.Equal("Vault", resolved!.Properties["text"]);
        Assert.Equal("VaultIcon", resolved.Properties["icon"]);
        // Non-overridden master property survives.
        Assert.Equal(true, resolved.Properties["enabled"]);
    }

    [Fact]
    public void Instance_children_append_after_master_children()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        doc.ReusableComponents[0].Children.Add(new NuiComponent { Id = "master_child", Type = "Icon" });
        var instance = new NuiComponent
        {
            Id = "taskbar_media",
            ComponentRef = "TaskbarButton",
            Children = { new NuiComponent { Id = "instance_child", Type = "Text" } },
        };

        var resolved = ReusableComponentResolver.Resolve(instance, doc);
        Assert.Equal(new[] { "master_child", "instance_child" }, resolved!.Children.Select(c => c.Id));
    }

    [Fact]
    public void Missing_master_resolves_to_null()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        var instance = new NuiComponent { Id = "ghost", ComponentRef = "NoSuchMaster" };
        Assert.Null(ReusableComponentResolver.Resolve(instance, doc));
    }

    [Fact]
    public void ComponentRef_and_overrides_survive_serialization()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "taskbar_vault",
            ComponentRef = "TaskbarButton",
            Overrides = new Dictionary<string, object?> { ["text"] = "Vault" },
        });

        var json = ProjectSerializer.Serialize(doc);
        Assert.Contains("\"componentRef\"", json);
        Assert.Contains("TaskbarButton", json);
        Assert.Contains("\"overrides\"", json);
        Assert.Contains("Vault", json);

        var reloaded = ProjectSerializer.Deserialize(json);
        var instance = reloaded.Screens[0].Root.Children.Single();
        Assert.Equal("TaskbarButton", instance.ComponentRef);
        Assert.Equal("Vault", instance.Overrides["text"]);
        // Clone carries the ref too.
        var cloned = instance.Clone();
        Assert.Equal("TaskbarButton", cloned.ComponentRef);
        Assert.Equal("Vault", cloned.Overrides["text"]);
    }

    [Fact]
    public void Master_change_propagates_to_resolution()
    {
        var doc = DocumentWithTaskbarButtonMaster();
        var instance = new NuiComponent { Id = "tb", ComponentRef = "TaskbarButton" };
        Assert.Equal("App", ReusableComponentResolver.Resolve(instance, doc)!.Properties["text"]);

        // Change the master once -> every instance reflects it.
        doc.ReusableComponents[0].Properties["text"] = "New Label";
        Assert.Equal("New Label", ReusableComponentResolver.Resolve(instance, doc)!.Properties["text"]);
    }
}
