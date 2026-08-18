namespace Nyforge.Core.Nui;

/// <summary>
/// Pure recursive operations over a <see cref="NuiCondition"/> tree
/// (NUI-SCHEMA §7.3) — the model-level half of the node-graph Logic
/// Editor. A condition is a leaf (an <c>expression</c> or the legacy
/// <c>state</c>/<c>operator</c>/<c>value</c> equality form) or a
/// <c>logic: and|or</c> group whose entries are each a leaf or a group.
///
/// These helpers mirror the Nyrqis floor's recursion
/// (<c>_eval_condition</c> / <c>_validate_condition</c>) so an editor
/// tree and the runtime gate agree on what the tree means; they live in
/// Nyforge.Core per NFC-001 §5.1 (pure logic, reusable by a future
/// runtime or test harness) — the Avalonia wrappers in Nyforge.Shell
/// are only the host-specific presentation.
/// </summary>
public static class NuiConditionTree
{
    public static bool IsGroup(NuiCondition? condition) =>
        condition is { Logic: not null };

    /// <summary>The node count of the tree (a group counts as 1 plus its
    /// children, recursively; a leaf counts as 1).</summary>
    public static int Count(NuiCondition? condition)
    {
        if (condition is null) return 0;
        var total = 1;
        if (IsGroup(condition) && condition.Conditions is { } children)
        {
            foreach (var child in children) total += Count(child);
        }
        return total;
    }

    /// <summary>Depth-first walk yielding (condition, depth) pairs, roots
    /// first — the order the editor and tests pin.</summary>
    public static IEnumerable<(NuiCondition Condition, int Depth)> Flatten(NuiCondition? condition)
    {
        if (condition is null) yield break;
        yield return (condition, 0);
        if (IsGroup(condition) && condition.Conditions is { } children)
        {
            foreach (var child in children)
            {
                foreach (var pair in Flatten(child))
                {
                    yield return (pair.Condition, pair.Depth + 1);
                }
            }
        }
    }

    /// <summary>Every leaf in depth-first order.</summary>
    public static IEnumerable<NuiCondition> Leaves(NuiCondition? condition) =>
        Flatten(condition)
            .Where(pair => !IsGroup(pair.Condition))
            .Select(pair => pair.Condition);

    /// <summary>The maximum nesting depth (a single leaf is depth 0; a
    /// group containing a leaf is depth 1).</summary>
    public static int MaxDepth(NuiCondition? condition) =>
        Flatten(condition).Select(pair => pair.Depth).DefaultIfEmpty(0).Max();

    /// <summary>A fresh empty AND/OR group (no children — validation
    /// requires a non-empty list before a document can ship).</summary>
    public static NuiCondition CreateGroup(string logic) => new()
    {
        Logic = logic is "or" ? "or" : "and",
        Conditions = new List<NuiCondition>(),
    };

    /// <summary>A fresh empty leaf — the editor fills in either the
    /// <c>expression</c> or the equality fields.</summary>
    public static NuiCondition CreateLeaf() => new() { Operator = "equals" };

    /// <summary>Valid logic labels (NUI-SCHEMA §7.3).</summary>
    public static readonly IReadOnlyList<string> LogicLabels = new[] { "and", "or" };

    public static string LogicLabel(NuiCondition? condition) =>
        IsGroup(condition) ? condition!.Logic! : "leaf";

    public static bool HasLogic(NuiCondition? condition, string logic) =>
        IsGroup(condition) && condition!.Logic == logic;

    /// <summary>
    /// The human-readable summary of the tree — the same shape the
    /// floor's evaluation describes: <c>and(leaf1, leaf2)</c> etc.
    /// Leaves render as their expression, or <c>state op value</c>.
    /// </summary>
    public static string Describe(NuiCondition? condition)
    {
        if (condition is null) return "always";
        if (IsGroup(condition))
        {
            var inner = condition.Conditions is { } children && children.Count > 0
                ? string.Join(", ", children.Select(Describe))
                : "…";
            return $"{condition.Logic}({inner})";
        }
        if (!string.IsNullOrEmpty(condition.Expression))
        {
            return condition.Expression;
        }
        var op = condition.Operator == "notEquals" ? "!=" : "==";
        return $"{condition.State} {op} {condition.Value}";
    }
}
