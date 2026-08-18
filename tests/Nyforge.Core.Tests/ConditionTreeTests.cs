using Nyforge.Core.Nui;
using Xunit;

namespace Nyforge.Core.Tests;

/// <summary>
/// The pure recursive condition-tree helpers (NUI-SCHEMA §7.3) the
/// node-graph Logic Editor's ConditionNodeViewModel builds on — count,
/// depth-first flatten, leaves, and describe. These mirror the Nyrqis
/// floor's recursion (_eval_condition / _validate_condition), so the
/// editor tree and the runtime gate agree on what a group means.
/// </summary>
public class ConditionTreeTests
{
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

    // ---- count --------------------------------------------------------------

    [Fact]
    public void Count_is_one_for_a_leaf_and_zero_for_null()
    {
        Assert.Equal(0, NuiConditionTree.Count(null));
        Assert.Equal(1, NuiConditionTree.Count(Leaf("dnd", true)));
    }

    [Fact]
    public void Count_recurses_through_groups()
    {
        var tree = Group("and",
            Leaf("dnd", true),
            Group("or", Leaf("volume", 60), Leaf("theme", "Eclipse")));
        // 1 (root and) + 1 (dnd) + 1 (or) + 2 (leaves) = 5
        Assert.Equal(5, NuiConditionTree.Count(tree));
    }

    // ---- flatten / leaves / depth -------------------------------------------

    [Fact]
    public void Flatten_is_depth_first_roots_first()
    {
        var tree = Group("or",
            Group("and", Leaf("a", 1), Leaf("b", 2)),
            Leaf("c", 3));

        var order = NuiConditionTree.Flatten(tree).Select(p => p.Condition).ToList();

        Assert.Equal("or", order[0].Logic);
        Assert.Equal("and", order[1].Logic);
        Assert.Equal("a", order[2].State);
        Assert.Equal("b", order[3].State);
        Assert.Equal("c", order[4].State);
    }

    [Fact]
    public void Flatten_reports_nesting_depth()
    {
        var tree = Group("and",
            Leaf("a", 1),
            Group("or", Leaf("b", 2)));

        var depths = NuiConditionTree.Flatten(tree).Select(p => p.Depth).ToList();
        Assert.Equal(new[] { 0, 1, 1, 2 }, depths);
        Assert.Equal(2, NuiConditionTree.MaxDepth(tree));
        Assert.Equal(0, NuiConditionTree.MaxDepth(Leaf("a", 1)));
        Assert.Equal(0, NuiConditionTree.MaxDepth(null));
    }

    [Fact]
    public void Leaves_returns_only_leaves_depth_first()
    {
        var tree = Group("and",
            Leaf("a", 1),
            Group("or", Leaf("b", 2), Leaf("c", 3)));

        Assert.Equal(new[] { "a", "b", "c" },
            NuiConditionTree.Leaves(tree).Select(l => l.State).ToArray());
    }

    // ---- construction -------------------------------------------------------

    [Fact]
    public void CreateGroup_defaults_to_and_with_empty_children()
    {
        var group = NuiConditionTree.CreateGroup("or");
        Assert.Equal("or", group.Logic);
        Assert.NotNull(group.Conditions);
        Assert.Empty(group.Conditions!);

        var invalid = NuiConditionTree.CreateGroup("xor");
        Assert.Equal("and", invalid.Logic); // anything not "or" defaults to and
    }

    [Fact]
    public void CreateLeaf_is_an_equality_leaf()
    {
        var leaf = NuiConditionTree.CreateLeaf();
        Assert.Null(leaf.Logic);
        Assert.Equal("equals", leaf.Operator);
        Assert.Null(leaf.Expression);
    }

    [Fact]
    public void IsGroup_and_LogicLabel_follow_the_logic_field()
    {
        Assert.True(NuiConditionTree.IsGroup(Group("and")));
        Assert.False(NuiConditionTree.IsGroup(Leaf("a", 1)));
        Assert.False(NuiConditionTree.IsGroup(null));
        Assert.Equal("and", NuiConditionTree.LogicLabel(Group("and")));
        Assert.Equal("leaf", NuiConditionTree.LogicLabel(Leaf("a", 1)));
        Assert.True(NuiConditionTree.HasLogic(Group("or"), "or"));
        Assert.False(NuiConditionTree.HasLogic(Leaf("a", 1), "and"));
    }

    // ---- describe -----------------------------------------------------------

    [Fact]
    public void Describe_covers_all_node_kinds()
    {
        Assert.Equal("always", NuiConditionTree.Describe(null));
        Assert.Equal("dnd == True", NuiConditionTree.Describe(Leaf("dnd", true)));
        Assert.Equal("volume != 60", NuiConditionTree.Describe(Leaf("volume", 60, "notEquals")));
        Assert.Equal("state.volume > 50",
            NuiConditionTree.Describe(new NuiCondition { Expression = "state.volume > 50" }));
        Assert.Equal("and(dnd == True, or(volume == 60, theme == Eclipse))",
            NuiConditionTree.Describe(Group("and",
                Leaf("dnd", true),
                Group("or", Leaf("volume", 60), Leaf("theme", "Eclipse")))));
    }
}
