using Nyforge.Core.Nui;

namespace Nyforge.Core.Runtime;

/// <summary>
/// Pure evaluation logic for a NuiCondition against a set of state values.
/// Deliberately has no dependency on Avalonia, NuiDocument mutation, or any
/// host-specific action dispatch — just the yes/no question "does this
/// condition currently hold?" This is the part of "running" a behavior
/// that isn't host-specific, so it lives in Nyforge.Core (NFC-001 §5.1)
/// rather than Nyforge.Shell, and a future Nyrqis UI Runtime could reuse it
/// verbatim rather than reimplementing condition semantics.
///
/// Executing the resulting action is host-specific (it means something
/// different in Forge's preview stand-in than it will in a real Nyrqis
/// process) and stays in Nyforge.Shell's BehaviorDispatcher.
/// </summary>
public static class BehaviorEvaluator
{
    public static bool Evaluate(NuiCondition? condition, IReadOnlyDictionary<string, object?> states)
    {
        if (condition is null) return true; // no condition => always runs

        // AND/OR logic group (NUI-SCHEMA §7.3): combine the
        // sub-conditions recursively — the internal representation the
        // visual logic-graph editor builds on. Mirrors the floor's
        // `all(sub) if logic == "and" else any(sub)`.
        if (!string.IsNullOrEmpty(condition.Logic))
        {
            var results = (condition.Conditions ?? Enumerable.Empty<NuiCondition>())
                .Select(sub => Evaluate(sub, states))
                .ToList();
            return condition.Logic == "and"
                ? results.All(x => x)
                : results.Any(x => x);
        }

        // Expression conditions (NUI-SCHEMA §7.2) supersede the legacy
        // equality form — the same expression string evaluates
        // identically in NyForge, the reference floor, and the Rust
        // crate (NExpr mirrors them byte-for-byte).
        if (!string.IsNullOrEmpty(condition.Expression))
        {
            return NExpr.Evaluate(condition.Expression, states) is true;
        }

        var hasValue = states.TryGetValue(condition.State, out var actual);
        var actualText = hasValue ? actual?.ToString() : null;
        var expectedText = condition.Value?.ToString();

        var equal = string.Equals(actualText, expectedText, StringComparison.Ordinal);

        return condition.Operator switch
        {
            "notEquals" => !equal,
            _ => equal, // "equals" and any unrecognized operator default to equality, per NUI-SCHEMA.md §7
        };
    }
}
