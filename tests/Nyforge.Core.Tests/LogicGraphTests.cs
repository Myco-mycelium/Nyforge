using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// Behavior logic graphs (NUI-SCHEMA §7.3) — nested AND/OR condition
/// groups and action chains: the internal representation the visual
/// logic-graph editor builds on. Mirrors the Nyrqis floor's
/// `_eval_condition` (all/any recursion) and the crate's fail-closed
/// validation; the C# model round-trips through the serializer without
/// serialization noise (no stray empty `actions` / `conditions` arrays).
/// </summary>
public class LogicGraphTests
{
    private static NuiDocument Blank() => NyforgeProject.CreateBlank();

    private static NuiCondition Leaf(string state, object? value, string op = "equals") => new()
    {
        State = state,
        Operator = op,
        Value = value,
    };

    private static NuiCondition Group(string logic, params NuiCondition[] conditions) => new()
    {
        Logic = logic,
        Conditions = conditions.ToList(),
    };

    // ---- evaluation ---------------------------------------------------------

    [Fact]
    public void And_group_is_true_only_when_all_conditions_hold()
    {
        var states = new Dictionary<string, object?> { ["dnd"] = true, ["theme"] = "Eclipse" };

        var allTrue = Group("and", Leaf("dnd", true), Leaf("theme", "Eclipse"));
        Assert.True(BehaviorEvaluator.Evaluate(allTrue, states));

        var oneFalse = Group("and", Leaf("dnd", true), Leaf("theme", "Solar"));
        Assert.False(BehaviorEvaluator.Evaluate(oneFalse, states));
    }

    [Fact]
    public void Or_group_is_true_when_any_condition_holds()
    {
        var states = new Dictionary<string, object?> { ["dnd"] = true, ["theme"] = "Eclipse" };

        var anyTrue = Group("or", Leaf("dnd", false), Leaf("theme", "Eclipse"));
        Assert.True(BehaviorEvaluator.Evaluate(anyTrue, states));

        var noneTrue = Group("or", Leaf("dnd", false), Leaf("theme", "Solar"));
        Assert.False(BehaviorEvaluator.Evaluate(noneTrue, states));
    }

    [Fact]
    public void Nested_groups_evaluate_recursively()
    {
        var states = new Dictionary<string, object?> { ["dnd"] = false, ["volume"] = 60, ["clockTime"] = "14:32" };

        // or( and(dnd == true, volume > 50), clockTime == "14:32" ) -> true
        var nested = Group("or",
            Group("and", Leaf("dnd", true), Leaf("volume", 60)),
            Leaf("clockTime", "14:32"));
        Assert.True(BehaviorEvaluator.Evaluate(nested, states));

        // or( and(dnd == true, volume > 50), clockTime == "09:00" ) -> false
        var noneTrue = Group("or",
            Group("and", Leaf("dnd", true), Leaf("volume", 60)),
            Leaf("clockTime", "09:00"));
        Assert.False(BehaviorEvaluator.Evaluate(noneTrue, states));
    }

    [Fact]
    public void Null_condition_always_runs()
    {
        Assert.True(BehaviorEvaluator.Evaluate(null, new Dictionary<string, object?>()));
    }

    // ---- serialization ------------------------------------------------------

    [Fact]
    public void Chain_serializes_as_actions_array_and_round_trips()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "behavior_apply_theme",
            Actions = new List<NuiAction>
            {
                new() { Target = "System", Name = "Nyrqis.Theme.Set", Arguments = { ["theme"] = "Solar" } },
                new() { Target = "System", Name = "Nyrqis.Settings.Commit" },
            },
        });

        var json = ProjectSerializer.Serialize(doc);
        Assert.DoesNotContain("\"action\"", json);
        Assert.Contains("\"actions\"", json);

        var reloaded = ProjectSerializer.Deserialize(json);
        var behavior = Assert.Single(reloaded.Behaviors);
        Assert.Null(behavior.Action);
        Assert.NotNull(behavior.Actions);
        Assert.Equal(2, behavior.Actions!.Count);
        Assert.Equal("Nyrqis.Theme.Set", behavior.Actions[0].Name);
        Assert.Equal("Nyrqis.Settings.Commit", behavior.Actions[1].Name);
    }

    [Fact]
    public void Single_action_serializes_without_actions_noise()
    {
        var behavior = new NuiBehavior
        {
            Id = "behavior_commit",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        };
        var doc = Blank();
        doc.Behaviors.Add(behavior);

        var json = ProjectSerializer.Serialize(doc);
        Assert.DoesNotContain("\"actions\"", json);
        Assert.Contains("\"action\"", json);

        var reloaded = ProjectSerializer.Deserialize(json);
        var round = Assert.Single(reloaded.Behaviors);
        Assert.NotNull(round.Action);
        Assert.Null(round.Actions);
    }

    [Fact]
    public void Leaf_condition_serializes_without_conditions_noise()
    {
        // ProjectSerializer (WhenWritingNull) omits the unused `conditions`
        // collection entirely — no `"conditions": []` noise on leaves —
        // while logic groups carry their array.
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = Group("and", Leaf("dnd", true), Leaf("theme", "Eclipse")),
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        });

        var json = ProjectSerializer.Serialize(doc);
        Assert.Contains("\"conditions\": [", json);
        Assert.DoesNotContain("\"conditions\": []", json);

        doc.Behaviors[0].Condition = Leaf("theme", "Eclipse");
        var leafJson = ProjectSerializer.Serialize(doc);
        Assert.DoesNotContain("\"conditions\"", leafJson);
    }

    // ---- validation ---------------------------------------------------------

    [Fact]
    public void Unknown_logic_operator_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Logic = "xor", Conditions = new List<NuiCondition> { Leaf("dnd", true) } },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-024" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Empty_conditions_group_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Logic = "and" },
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-024" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Both_action_and_actions_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
            Actions = new List<NuiAction>
            {
                new() { Target = "System", Name = "Nyrqis.Settings.Commit" },
            },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-024" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Neither_action_nor_actions_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior { Id = "b1" });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-024" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Unknown_state_in_nested_group_is_error()
    {
        var doc = Blank();
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = Group("or",
                Group("and", Leaf("dnd", true), Leaf("ghost_state", 1)),
                Leaf("theme", "Eclipse")),
            Action = new NuiAction { Target = "System", Name = "Nyrqis.Settings.Commit" },
        });

        var result = NuiValidator.Validate(doc);

        Assert.Contains(result.Errors, i => i.Code == "ER-NUI-005" && i.BehaviorId == "b1");
    }

    [Fact]
    public void Valid_chain_and_group_pass()
    {
        var doc = Blank();
        doc.States["dnd"] = false;
        doc.States["theme"] = "Eclipse";
        doc.Behaviors.Add(new NuiBehavior
        {
            Id = "b1",
            Condition = Group("and", Leaf("dnd", false), Leaf("theme", "Eclipse")),
            Actions = new List<NuiAction>
            {
                new() { Target = "System", Name = "Nyrqis.Theme.Set", Arguments = { ["theme"] = "Solar" } },
                new() { Target = "System", Name = "Nyrqis.Settings.Commit" },
            },
        });

        var result = NuiValidator.Validate(doc);

        Assert.DoesNotContain(result.Errors, i => i.Code is "ER-NUI-005" or "ER-NUI-024"
            or "ER-NUI-007" or "ER-NUI-008");
    }
}
