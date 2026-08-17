using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The design-time NUI validator (check-before-Preview): every example
/// fixture must validate with zero errors (it would fail the Nyrqis
/// import gate otherwise), and each finding code is pinned by a unit
/// case so the lint contract can't drift.
/// </summary>
public class NuiValidatorTests
{
    private static NuiDocument Blank() => NyforgeProject.CreateBlank();

    private static NuiComponent Btn(string id, string? behavior = null) => new()
    {
        Id = id,
        Type = "Button",
        Properties = new Dictionary<string, object?> { ["text"] = "Go" },
        Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        Events = behavior is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?> { ["clicked"] = behavior },
    };

    // ---- errors -------------------------------------------------------------

    [Fact]
    public void Unknown_type_is_error()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(new NuiComponent { Id = "x", Type = "BogusWidget" });

        var result = NuiValidator.Validate(doc);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-001" && i.ComponentId == "x");
    }

    [Fact]
    public void Unknown_property_is_error()
    {
        var doc = Blank();
        var btn = Btn("btn_save");
        btn.Properties["bogus"] = 1;
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-002" && i.ComponentId == "btn_save");
    }

    [Fact]
    public void Unknown_event_is_error()
    {
        var doc = Blank();
        var btn = Btn("btn_save");
        btn.Events["hovered"] = null;
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-003" && i.ComponentId == "btn_save");
    }

    [Fact]
    public void Dangling_behavior_reference_is_error()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(Btn("btn_save", "behavior_missing"));

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-004" && i.BehaviorId == "behavior_missing");
    }

    [Fact]
    public void Condition_referencing_deleted_state_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { State = "theme", Operator = "equals", Value = "Eclipse" },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Notification.Show" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-005" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Action_targeting_missing_component_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "ghost", Name = "Close" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-006" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Unknown_system_action_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.System.Shutdown" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-007" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Unknown_action_argument_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?> { ["bogus"] = 1 },
            },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-008" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Dangling_binding_component_is_error()
    {
        var doc = Blank();
        doc.Bindings.Add(new NuiBinding { ComponentId = "ghost", Property = "text", State = "theme" });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-009");
    }

    [Fact]
    public void Dangling_binding_state_is_error()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(Btn("btn_save"));
        doc.Bindings.Add(new NuiBinding { ComponentId = "btn_save", Property = "text", State = "nope" });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-010");
    }

    [Fact]
    public void Dangling_component_ref_is_error()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "inst",
            ComponentRef = "GhostMaster",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-011" && i.ComponentId == "inst");
    }

    [Fact]
    public void Instance_declaring_own_type_is_error()
    {
        var doc = Blank();
        doc.ReusableComponents.Add(new NuiComponent { Id = "Master", Type = "Button" });
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "inst",
            ComponentRef = "Master",
            Type = "Button",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-012" && i.ComponentId == "inst");
    }

    [Fact]
    public void Override_outside_master_contract_is_error()
    {
        var doc = Blank();
        doc.ReusableComponents.Add(new NuiComponent { Id = "Master", Type = "Button" });
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "inst",
            ComponentRef = "Master",
            Overrides = new Dictionary<string, object?> { ["bogus"] = true },
            Layout = new NuiLayout { X = 0, Y = 0, Width = 96, Height = 48 },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-013" && i.ComponentId == "inst");
    }

    [Fact]
    public void Invalid_layout_constraints_are_errors()
    {
        var doc = Blank();
        var btn = Btn("btn_bad");
        btn.Layout.MinWidth = 2000;
        btn.Layout.MaxWidth = 1000; // min > max
        btn.Layout.AspectRatio = -1;
        doc.Screens[0].Root.Children.Add(btn);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-017"); // min > max
        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-018"); // bad aspect
    }

    [Fact]
    public void Component_action_not_in_contract_is_error()
    {
        var doc = Blank();
        var window = doc.Screens[0].Root; // Window contract: actions [Close]
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = window.Id, Name = "Minimize" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-015" && i.BehaviorId == "b1");
    }

    // ---- warnings -----------------------------------------------------------

    [Fact]
    public void Duplicate_component_id_is_warning()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(Btn("btn_save"));
        doc.Screens[0].Root.Children.Add(Btn("btn_save"));

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-001");
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Child_overflowing_parent_is_warning()
    {
        var doc = Blank();
        var container = new NuiComponent
        {
            Id = "panel",
            Type = "Container",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 200, Height = 100 },
        };
        container.Children.Add(new NuiComponent
        {
            Id = "wide",
            Type = "Button",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 300, Height = 40 },
        });
        doc.Screens[0].Root.Children.Add(container);

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-004" && i.ComponentId == "wide");
    }

    [Fact]
    public void Missing_image_source_is_warning()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "img",
            Type = "Image",
            Properties = new Dictionary<string, object?> { ["source"] = "nope.png" },
        });

        var result = NuiValidator.Validate(doc, projectDirectory: "/nonexistent/dir");

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-005" && i.ComponentId == "img");
    }

    [Fact]
    public void Unused_behavior_is_warning()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "orphan",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Notification.Show" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Warnings, i => i.Code == "WN-NUI-006" && i.BehaviorId == "orphan");
    }

    // ---- info ---------------------------------------------------------------

    [Fact]
    public void Duplicate_structure_is_reuse_candidate()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(Btn("btn_one"));
        doc.Screens[0].Root.Children.Add(Btn("btn_two"));

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Infos, i => i.Code == "IN-NUI-001");
        Assert.False(result.HasErrors);
    }

    // ---- clean document -----------------------------------------------------

    [Fact]
    public void Clean_document_has_no_errors()
    {
        var doc = Blank();
        doc.Screens[0].Root.Children.Add(Btn("btn_save", "b1"));
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Notification.Show" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.False(result.HasErrors);
    }

    // ---- example fixtures (the CI gate) --------------------------------------

    public static TheoryData<string> ExampleFixtures()
    {
        var data = new TheoryData<string>();
        var examples = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples"));
        foreach (var file in Directory.EnumerateFiles(examples, "*.nstudio",
                     SearchOption.AllDirectories))
        {
            data.Add(file);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ExampleFixtures))]
    public void Every_example_fixture_validates_with_zero_errors(string path)
    {
        var json = File.ReadAllText(path);
        var doc = ProjectSerializer.Deserialize(json);

        var result = NuiValidator.Validate(doc, Path.GetDirectoryName(path));

        Assert.False(result.HasErrors,
            $"{Path.GetFileName(path)} has errors:\n" +
            string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}")));
    }
}
