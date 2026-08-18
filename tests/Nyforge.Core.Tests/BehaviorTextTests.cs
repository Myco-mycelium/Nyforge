using Nyforge.Core.Nui;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The compact text format for NuiBehavior (NUI-SCHEMA §7.3) — the model
/// surface behind the "advanced code mode" editor toggle. Both the visual
/// node-graph and code (text) editing surfaces produce the same
/// NuiBehavior; this class tests that round-trip.
/// </summary>
public class BehaviorTextTests
{
    // ---- serialization ------------------------------------------------------

    [Fact]
    public void Serialize_simple_no_condition_single_action()
    {
        var b = new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        };

        var text = NuiBehaviorText.Serialize(b, "taskbar", "start_menu.Opened");

        Assert.Contains("WHEN taskbar start_menu.Opened", text);
        Assert.Contains("DO System.Nyrqis.Settings.Commit", text);
        Assert.DoesNotContain("IF", text);
    }

    [Fact]
    public void Serialize_legacy_equality_condition()
    {
        var b = new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { State = "theme", Operator = "equals", Value = "Eclipse" },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Theme.Set" },
        };

        var text = NuiBehaviorText.Serialize(b, "toggle", "changed");

        Assert.Contains("IF theme == Eclipse", text);
    }

    [Fact]
    public void Serialize_expression_condition()
    {
        var b = new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Expression = "state.volume > 50" },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        };

        var text = NuiBehaviorText.Serialize(b, "slider", "changed");

        Assert.Contains("IF state.volume > 50", text);
    }

    [Fact]
    public void Serialize_group_condition()
    {
        var b = new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition
            {
                Logic = "and",
                Conditions = new List<NuiCondition>
                {
                    new() { State = "dnd", Operator = "equals", Value = true },
                    new() { Expression = "state.volume > 50 && state.active" },
                },
            },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        };

        var text = NuiBehaviorText.Serialize(b, "comp", "event");

        Assert.Contains("IF and(", text);
        Assert.Contains("dnd == True", text);
        Assert.Contains("state.volume > 50 && state.active", text);
    }

    [Fact]
    public void Serialize_action_chain()
    {
        var b = new NuiBehavior
        {
            Id = "b1",
            Condition = null,
            Actions = new List<NuiAction>
            {
                new() { Target = "System", Name = "Nyrqis.Theme.Set",
                    Arguments = { ["theme"] = "Solar" } },
                new() { Target = "System", Name = "Nyrqis.Settings.Commit" },
            },
        };

        var text = NuiBehaviorText.Serialize(b, "toggle", "changed");

        var lines = text.Split('\n').Where(l => l.StartsWith("DO ", StringComparison.Ordinal)).ToList();
        Assert.Equal(2, lines.Count);
        Assert.Contains("DO System.Nyrqis.Theme.Set theme=Solar", lines[0]);
        Assert.Contains("DO System.Nyrqis.Settings.Commit", lines[1]);
    }

    // ---- parsing ------------------------------------------------------------

    [Fact]
    public void Parse_simple_no_condition_single_action()
    {
        var text = "WHEN taskbar StartMenu.Opened\nDO System.Nyrqis.Settings.Commit";
        Assert.True(NuiBehaviorText.TryParse(text, out var b, out var cid, out var evt, out var err));
        Assert.Null(err);
        Assert.Equal("taskbar", cid);
        Assert.Equal("StartMenu.Opened", evt);
        Assert.Null(b!.Condition);
        Assert.NotNull(b.Action);
        Assert.Equal("System", b.Action!.Target);
        Assert.Equal("Nyrqis.Settings.Commit", b.Action.Name);
    }

    [Fact]
    public void Parse_legacy_equality_condition()
    {
        var text = "WHEN toggle changed\nIF theme == Eclipse\nDO System.Nyrqis.Theme.Set";
        Assert.True(NuiBehaviorText.TryParse(text, out var b, out _, out _, out _));
        Assert.NotNull(b!.Condition);
        Assert.Equal("theme", b.Condition!.State);
        Assert.Equal("equals", b.Condition.Operator);
        Assert.Equal("Eclipse", b.Condition.Value);
    }

    [Fact]
    public void Parse_expression_condition()
    {
        var text = "WHEN slider changed\nIF state.volume > 50\nDO System.Nyrqis.Settings.Commit";
        Assert.True(NuiBehaviorText.TryParse(text, out var b, out _, out _, out _));
        Assert.Equal("state.volume > 50", b!.Condition!.Expression);
    }

    [Fact]
    public void Parse_group_condition()
    {
        var text = "WHEN comp event\nIF and(\n  state.dnd == true && state.active,\n  state.volume > 50\n)\nDO System.Action";
        Assert.True(NuiBehaviorText.TryParse(text, out var b, out _, out _, out var err), err);
        Assert.Equal("and", b!.Condition!.Logic);
        Assert.Equal(2, b.Condition.Conditions!.Count);
        Assert.Equal("state.dnd == true && state.active", b.Condition.Conditions![0].Expression);
        Assert.Equal("state.volume > 50", b.Condition.Conditions[1].Expression);
    }

    [Fact]
    public void Parse_action_chain()
    {
        var text = "WHEN toggle changed\nDO System.Nyrqis.Theme.Set theme=Eclipse\nDO System.Nyrqis.Animation.Play animation=fade";
        Assert.True(NuiBehaviorText.TryParse(text, out var b, out _, out _, out _));
        Assert.Null(b!.Action);
        Assert.NotNull(b.Actions);
        Assert.Equal(2, b.Actions!.Count);
        Assert.Equal("Nyrqis.Theme.Set", b.Actions[0].Name);
        Assert.Equal("Eclipse", b.Actions[0].Arguments["theme"]);
        Assert.Equal("Nyrqis.Animation.Play", b.Actions[1].Name);
        Assert.Equal("fade", b.Actions[1].Arguments["animation"]);
    }

    // ---- round-trip ---------------------------------------------------------

    [Fact]
    public void Round_trip_single_action_no_condition()
    {
        var original = new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        };
        var text = NuiBehaviorText.Serialize(original, "comp", "event");
        Assert.True(NuiBehaviorText.TryParse(text, out var parsed, out _, out _, out _));
        Assert.Equal("System", parsed!.Action!.Target);
        Assert.Equal("Nyrqis.Settings.Commit", parsed.Action.Name);
    }

    [Fact]
    public void Round_trip_chain_with_group_condition()
    {
        var original = new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition
            {
                Logic = "or",
                Conditions = new List<NuiCondition>
                {
                    new() { Expression = "state.dnd == true && state.active" },
                    new() { State = "theme", Operator = "notEquals", Value = "Eclipse" },
                },
            },
            Actions = new List<NuiAction>
            {
                new() { Target = "System", Name = "Nyrqis.Theme.Set",
                    Arguments = { ["theme"] = "Solar" } },
                new() { Target = "System", Name = "Nyrqis.Settings.Commit" },
            },
        };
        var text = NuiBehaviorText.Serialize(original, "toggle", "changed");
        Assert.True(NuiBehaviorText.TryParse(text, out var parsed, out var cid, out var evt, out var err));
        Assert.Null(err);
        Assert.Equal("toggle", cid);
        Assert.Equal("changed", evt);
        Assert.Equal("or", parsed!.Condition!.Logic);
        Assert.Equal(2, parsed.Condition.Conditions!.Count);
        Assert.Equal("state.dnd == true && state.active", parsed.Condition.Conditions[0].Expression);
        Assert.Equal("theme", parsed.Condition.Conditions[1].State);
        Assert.Equal("notEquals", parsed.Condition.Conditions[1].Operator);
        Assert.Equal(2, parsed.Actions!.Count);
    }

    // ---- parse errors -------------------------------------------------------

    [Fact]
    public void Parse_error_missing_when()
    {
        var text = "DO System.Action";
        Assert.False(NuiBehaviorText.TryParse(text, out _, out _, out _, out var err));
        Assert.Contains("missing WHEN line", err!);
    }

    [Fact]
    public void Parse_error_no_actions()
    {
        var text = "WHEN comp event";
        Assert.False(NuiBehaviorText.TryParse(text, out _, out _, out _, out var err));
        Assert.Contains("at least one DO line", err!);
    }

    [Fact]
    public void Parse_error_bad_action_format()
    {
        var text = "WHEN comp event\nDO System";
        Assert.False(NuiBehaviorText.TryParse(text, out _, out _, out _, out var err));
        Assert.Contains("target.actionName", err!);
    }

    [Fact]
    public void Parse_error_empty_text()
    {
        Assert.False(NuiBehaviorText.TryParse("", out _, out _, out _, out var err));
        Assert.Contains("empty text", err!);
    }
}
