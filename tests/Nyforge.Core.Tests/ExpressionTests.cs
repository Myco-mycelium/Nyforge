using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The NUI expression language (NUI-SCHEMA §7.2) — NExpr, the C# mirror
/// of the Nyrqis reference floor and Rust crate. Same grammar, same
/// precedence, same error messages: a screen that evaluates here must
/// evaluate identically on the runtime side. Also covers the validator's
/// ER-NUI-021 gate and the BehaviorEvaluator / ActionArgumentResolver
/// wiring.
/// </summary>
public class ExpressionTests
{
    private static readonly IReadOnlyDictionary<string, object?> States =
        new Dictionary<string, object?>
        {
            ["doNotDisturb"] = true,
            ["volume"] = 60,
            ["clockTime"] = "14:32",
            ["themeName"] = "Eclipse",
        };

    // ---- evaluation ---------------------------------------------------------

    [Theory]
    [InlineData("state.doNotDisturb == true", true)]
    [InlineData("state.volume > 50", true)]
    [InlineData("state.volume >= 60 && !state.doNotDisturb", false)]
    [InlineData("if(state.volume > 50, \"loud\", \"quiet\")", "loud")]
    [InlineData("min(state.volume, 100)", 60.0)]
    [InlineData("max(1, 2, 3)", 3.0)]
    [InlineData("contains(\"hello\", \"ell\")", true)]
    [InlineData("format(state.clockTime, \"{0}\")", "14:32")]
    [InlineData("format(state.volume, \"{0:.1f}\")", "60.0")]
    [InlineData("!false", true)]
    [InlineData("1 + 2 * 3", 7.0)]
    [InlineData("(state.volume - 10) * 2", 100.0)]
    [InlineData("\"a\" + \"b\"", "ab")]
    [InlineData("state.clockTime == \"14:32\"", true)]
    public void Evaluate_computes_expected_results(string expression, object? expected)
    {
        Assert.Equal(expected, NExpr.Evaluate(expression, States));
    }

    [Fact]
    public void Missing_state_evaluates_to_empty_string()
    {
        Assert.Equal(string.Empty, NExpr.Evaluate("state.ghost", States));
    }

    // ---- validation ---------------------------------------------------------

    [Fact]
    public void TryValidate_reports_unknown_state()
    {
        var problem = NExpr.TryValidate(
            "state.ghost > 1", new HashSet<string> { "volume" });
        Assert.Equal("expr: unknown state 'state.ghost'", problem);
    }

    [Fact]
    public void TryValidate_reports_syntax_error_with_position()
    {
        var problem = NExpr.TryValidate(
            "state.volume >", new HashSet<string> { "volume" });
        Assert.Equal("expr: syntax error at 14: unexpected token ''", problem);
    }

    [Fact]
    public void TryValidate_reports_unknown_function()
    {
        var problem = NExpr.TryValidate(
            "bogus(state.volume)", new HashSet<string> { "volume" });
        Assert.Equal("expr: unknown function 'bogus'", problem);
    }

    [Fact]
    public void TryValidate_reports_wrong_arity()
    {
        var problem = NExpr.TryValidate(
            "if(state.volume > 1)", new HashSet<string> { "volume" });
        Assert.Equal("expr: function 'if' expects 3 argument(s), got 1", problem);
    }

    [Fact]
    public void TryValidate_accepts_valid_expression()
    {
        Assert.Null(NExpr.TryValidate(
            "state.volume > 50 && !state.doNotDisturb",
            new HashSet<string> { "volume", "doNotDisturb" }));
    }

    // ---- validator (ER-NUI-021) ---------------------------------------------

    private static NuiDocument DocWithBehavior(NuiBehavior behavior)
    {
        var doc = NyforgeProject.CreateBlank();
        doc.States["volume"] = 60;
        doc.Behaviors.Add(behavior);
        return doc;
    }

    [Fact]
    public void Validator_rejects_expression_condition_unknown_state()
    {
        var doc = DocWithBehavior(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Expression = "state.ghost > 1" },
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
        var issue = Assert.Single(result.Errors);
        Assert.Equal("ER-NUI-021", issue.Code);
        Assert.Contains("unknown state 'state.ghost'", issue.Message);
        Assert.Equal("b1", issue.BehaviorId);
    }

    [Fact]
    public void Validator_rejects_expr_argument_unknown_state()
    {
        var doc = DocWithBehavior(new NuiBehavior
        {
            Id = "b1",
            Action = new NuiAction
            {
                Target = "System",
                Name = "Nyrqis.Notification.Show",
                Arguments = new Dictionary<string, object?>
                {
                    ["title"] = "$expr:state.ghost",
                    ["message"] = "x",
                    ["severity"] = "info",
                },
            },
        });
        var result = NuiValidator.Validate(doc);
        Assert.Contains(result.Errors, e =>
            e.Code == "ER-NUI-021" &&
            e.Message.Contains("behavior 'b1' argument") &&
            e.Message.Contains("unknown state 'state.ghost'"));
    }

    [Fact]
    public void Validator_accepts_valid_expression_condition()
    {
        var doc = DocWithBehavior(new NuiBehavior
        {
            Id = "b1",
            Condition = new NuiCondition { Expression = "state.volume > 50" },
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
        Assert.DoesNotContain(result.Errors, e => e.Code == "ER-NUI-021");
    }

    // ---- runtime wiring ------------------------------------------------------

    [Fact]
    public void BehaviorEvaluator_evaluates_expression_condition()
    {
        var condition = new NuiCondition { Expression = "state.volume > 50" };
        Assert.True(BehaviorEvaluator.Evaluate(condition, States));
        var falseCondition = new NuiCondition { Expression = "state.volume > 90" };
        Assert.False(BehaviorEvaluator.Evaluate(falseCondition, States));
    }

    [Fact]
    public void BehaviorEvaluator_legacy_condition_still_works()
    {
        var condition = new NuiCondition { State = "themeName", Operator = "equals", Value = "Eclipse" };
        Assert.True(BehaviorEvaluator.Evaluate(condition, States));
    }

    [Fact]
    public void ActionArgumentResolver_evaluates_expr_arguments()
    {
        var resolved = ActionArgumentResolver.Resolve(
            new Dictionary<string, object?>
            {
                ["title"] = "$expr:format(state.clockTime, \"{0}\")",
                ["message"] = "plain",
            },
            States);
        Assert.Equal("14:32", resolved["title"]);
        Assert.Equal("plain", resolved["message"]);
    }

    [Fact]
    public void Expression_condition_round_trips_through_json()
    {
        var condition = new NuiCondition { Expression = "state.volume > 50" };
        var json = System.Text.Json.JsonSerializer.Serialize(condition);
        var back = System.Text.Json.JsonSerializer.Deserialize<NuiCondition>(json);
        Assert.NotNull(back);
        Assert.Equal("state.volume > 50", back!.Expression);
    }

    // ---- example fixtures ----------------------------------------------------

    [Fact]
    public void Desktop_fixture_expression_condition_and_argument_validate()
    {
        var document = ProjectSerializer.LoadFromFile(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                         "examples", "nyrqis-shell", "desktop.nstudio"));
        var result = NuiValidator.Validate(document);
        Assert.DoesNotContain(result.Errors, e => e.Code == "ER-NUI-021");

        var behavior = document.Behaviors
            .First(b => b.Id == "behavior_dnd_on");
        Assert.Equal("state.doNotDisturb == true", behavior.Condition?.Expression);

        // The runtime state view is the flattened document (flat states
        // + every scope under its dotted names, NUI-SCHEMA §8.4) — the
        // clock lives in the session scope on this fixture.
        var states = document.FlattenedStates();
        Assert.False(BehaviorEvaluator.Evaluate(behavior.Condition, states));

        var args = ActionArgumentResolver.Resolve(
            behavior.Action.Arguments, states);
        Assert.Equal("14:32", args["title"]); // format(state.clockTime, "{0}")
    }
}
