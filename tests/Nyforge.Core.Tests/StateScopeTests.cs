using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// State scopes (NUI-SCHEMA §8.4): the stateScopes section — global /
/// screen / component / session / persistent — referenced as dotted
/// scope.key names in expressions, conditions, bindings, and arguments;
/// validated fail-closed in the editor and resolved at Preview runtime.
/// global is the named form of the flat states section.
/// </summary>
public class StateScopeTests
{
    private static NuiDocument DocWithScopes()
    {
        var doc = NyforgeProject.CreateBlank();
        doc.States["volume"] = 60;
        doc.StateScopes["persistent"] = new Dictionary<string, object?>
        {
            ["theme"] = "Eclipse",
        };
        doc.StateScopes["session"] = new Dictionary<string, object?>
        {
            ["clockTime"] = "14:32",
        };
        return doc;
    }

    // ---- resolution ---------------------------------------------------------

    [Fact]
    public void FlattenedStates_merges_scoped_entries_under_dotted_names()
    {
        var doc = DocWithScopes();
        var merged = doc.FlattenedStates();
        Assert.Equal("Eclipse", merged["persistent.theme"]);
        Assert.Equal("14:32", merged["session.clockTime"]);
        // Bare flat keys keep working.
        Assert.Equal(60, merged["volume"]);
    }

    [Fact]
    public void IsStateKnown_resolves_dotted_scoped_reference()
    {
        var doc = DocWithScopes();
        Assert.True(doc.IsStateKnown("persistent.theme"));
        Assert.True(doc.IsStateKnown("session.clockTime"));
        Assert.False(doc.IsStateKnown("persistent.ghost"));
        Assert.False(doc.IsStateKnown("bogus.theme"));
        Assert.False(doc.IsStateKnown("session.ghost"));
    }

    [Fact]
    public void IsStateKnown_resolves_flat_and_global()
    {
        var doc = DocWithScopes();
        Assert.True(doc.IsStateKnown("volume"));
        doc.StateScopes["global"] = new Dictionary<string, object?> { ["volume"] = 60 };
        Assert.True(doc.IsStateKnown("volume")); // flat wins but global also knows it
        Assert.True(doc.IsStateKnown("stateDoesNotExist") == false);
    }

    [Fact]
    public void FlattenedStates_flat_keys_win_on_collision()
    {
        var doc = DocWithScopes();
        doc.StateScopes["global"] = new Dictionary<string, object?> { ["volume"] = 99 };
        Assert.Equal(60, doc.FlattenedStates()["volume"]); // flat section wins
        Assert.Equal(99, doc.FlattenedStates()["global.volume"]);
    }

    // ---- validation ---------------------------------------------------------

    [Fact]
    public void Unknown_scope_is_an_error()
    {
        var doc = DocWithScopes();
        doc.StateScopes["bogus"] = new Dictionary<string, object?> { ["a"] = 1 };
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Issues, i =>
            i.Code == "ER-NUI-023" && i.Message.Contains("unknown scope 'bogus'"));
    }

    [Fact]
    public void Null_scope_table_is_an_error()
    {
        var doc = DocWithScopes();
        doc.StateScopes["persistent"] = null!;
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Issues, i =>
            i.Code == "ER-NUI-023" && i.Message.Contains("must be an object"));
    }

    [Fact]
    public void Expression_condition_accepts_scoped_states()
    {
        var doc = DocWithScopes();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Expression = "state.persistent.theme == \"Eclipse\"" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["message"] = "x",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Issues, i => i.Code == "ER-NUI-021");
    }

    [Fact]
    public void Expression_condition_unknown_scoped_state_is_an_error()
    {
        var doc = DocWithScopes();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Expression = "state.persistent.ghost == \"x\"" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["message"] = "x",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Issues, i =>
            i.Code == "ER-NUI-021" && i.Message.Contains("unknown state"));
    }

    [Fact]
    public void Legacy_condition_accepts_scoped_state()
    {
        var doc = DocWithScopes();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { State = "persistent.theme", Value = "Eclipse" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["message"] = "x",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Issues, i => i.Code == "ER-NUI-005");
    }

    [Fact]
    public void Legacy_condition_unknown_scoped_state_is_an_error()
    {
        var doc = DocWithScopes();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { State = "session.ghost", Value = "x" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["message"] = "x",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Issues, i => i.Code == "ER-NUI-005");
    }

    [Fact]
    public void Binding_accepts_scoped_state()
    {
        var doc = DocWithScopes();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "lbl",
            Type = "Text",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 10, Height = 10 },
        });
        doc.Bindings.Add(new NuiBinding { ComponentId = "lbl", Property = "text", State = "session.clockTime" });
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Issues, i => i.Code == "ER-NUI-010");
    }

    [Fact]
    public void Binding_unknown_scoped_state_is_an_error()
    {
        var doc = DocWithScopes();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "lbl",
            Type = "Text",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 10, Height = 10 },
        });
        doc.Bindings.Add(new NuiBinding { ComponentId = "lbl", Property = "text", State = "persistent.ghost" });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Issues, i => i.Code == "ER-NUI-010");
    }

    [Fact]
    public void Expr_argument_accepts_scoped_states()
    {
        var doc = DocWithScopes();
        doc.Screens[0].Root.Children.Add(new NuiComponent
        {
            Id = "btn",
            Type = "Button",
            Layout = new NuiLayout { X = 0, Y = 0, Width = 10, Height = 10 },
        });
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { State = "persistent.theme", Value = "Eclipse" },
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["message"] = "$expr:state.persistent.theme",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Issues, i => i.Code == "ER-NUI-021");
    }

    // ---- fixture ------------------------------------------------------------

    [Fact]
    public void Desktop_fixture_scopes_validate_and_resolve()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "nyrqis-shell");
        var path = Path.Combine(fixtures, "desktop.nstudio");
        if (!File.Exists(path)) return; // not running from the repo layout
        var doc = ProjectSerializer.LoadFromFile(path);
        var result = NuiValidator.Validate(doc);
        Assert.DoesNotContain(result.Issues, i => i.Severity == NuiIssueSeverity.Error);

        Assert.Equal("Eclipse", doc.FlattenedStates()["persistent.theme"]);
        Assert.True(doc.IsStateKnown("persistent.theme"));
        Assert.True(doc.IsStateKnown("session.clockTime"));
    }
}
