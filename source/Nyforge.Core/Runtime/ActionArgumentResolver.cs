using Nyforge.Core.Nui;

namespace Nyforge.Core.Runtime;

/// <summary>
/// Resolves "$state:key" placeholder values inside a NuiAction's arguments
/// against the current runtime state, so an action can say "use whatever
/// this state currently holds" rather than only ever a static literal.
/// See NUI-SCHEMA.md §7's "Expression-valued arguments" subsection.
///
/// Deliberately minimal: this is string-prefix substitution, not an
/// expression language. No ternaries, no string concatenation, no nested
/// lookups. A value that isn't a string, or doesn't start with "$state:",
/// passes through unchanged — it's a literal, exactly as before this
/// feature existed. Kept in Nyforge.Core (framework-free) alongside
/// BehaviorEvaluator, for the same reason: this is host-independent logic
/// a future Nyrqis UI Runtime could reuse as-is (NFC-001 §5.1).
/// </summary>
public static class ActionArgumentResolver
{
    private const string StatePrefix = "$state:";
    private const string ExprPrefix = "$expr:";

    /// <summary>
    /// Returns a new dictionary with every "$state:key" string value
    /// replaced by states[key] (or left as the literal placeholder string
    /// if that key doesn't exist — see the doc comment on why this doesn't
    /// silently produce null). Never mutates the input.
    /// </summary>
    public static Dictionary<string, object?> Resolve(
        IReadOnlyDictionary<string, object?> arguments,
        IReadOnlyDictionary<string, object?> states)
    {
        var resolved = new Dictionary<string, object?>(arguments.Count);

        foreach (var (key, value) in arguments)
        {
            resolved[key] = ResolveValue(value, states);
        }

        return resolved;
    }

    private static object? ResolveValue(object? value, IReadOnlyDictionary<string, object?> states)
    {
        if (value is not string text)
        {
            return value;
        }

        // NUI expression (NUI-SCHEMA §7.2): whole-string `$expr:` values
        // are evaluated against the current state by the expression
        // language — identical semantics in NyForge, the reference
        // floor, and the Rust crate.
        if (text.StartsWith(ExprPrefix, StringComparison.Ordinal))
        {
            var expression = text[ExprPrefix.Length..];
            return NExpr.Evaluate(expression, states);
        }

        if (!text.StartsWith(StatePrefix, StringComparison.Ordinal))
        {
            return value; // not a placeholder — a literal, unchanged
        }

        var stateKey = text[StatePrefix.Length..];

        // A reference to a state that doesn't exist is left as the literal
        // placeholder text rather than silently becoming null — a missing
        // state is a real authoring mistake worth surfacing (e.g. in
        // BehaviorDispatcher's event log), not something to paper over.
        return states.TryGetValue(stateKey, out var resolvedValue) ? resolvedValue : text;
    }
}
