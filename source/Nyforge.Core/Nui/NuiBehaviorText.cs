using System.Text;

namespace Nyforge.Core.Nui;

/// <summary>
/// A compact, human-readable text format for <see cref="NuiBehavior"/>
/// (NUI-SCHEMA §7.3) — the model surface behind the "advanced code mode"
/// editor toggle. Both visual (node-graph) and code (text) editing
/// surfaces produce the same <see cref="NuiBehavior"/> model (the
/// original design doc's "two development approaches, one API" rule);
/// this class is the code side.
///
/// Format (one line per directive):
/// <code>
/// WHEN componentId eventName
/// IF expression | state operator value | and(...) | or(...)
/// DO target.actionName key1=value1 key2=value2
/// DO target.actionName
/// </code>
///
/// The IF line is optional (omitted = no condition, behavior always runs).
/// Multiple DO lines = an action chain; a single DO = a single-action
/// behavior. The <c>WHEN</c> line carries the component/event binding
/// that makes the behavior reachable.
/// </summary>
public static class NuiBehaviorText
{
    // ---- serialization ------------------------------------------------------

    /// <summary>
    /// Serialize a behavior + its component/event binding to the compact
    /// text format. An empty string means the behavior has no actions.
    /// </summary>
    public static string Serialize(NuiBehavior behavior, string componentId, string eventName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"WHEN {componentId} {eventName}");

        if (behavior.Condition is { } condition)
        {
            sb.AppendLine($"IF {DescribeCondition(condition)}");
        }

        List<NuiAction> actions = behavior.Actions is { Count: > 0 } chain
            ? chain
            : behavior.Action is { } single ? new List<NuiAction> { single } : new();

        foreach (var action in actions)
        {
            sb.AppendLine(DescribeAction(action));
        }

        return sb.ToString().TrimEnd('\n');
    }

    private static string DescribeCondition(NuiCondition c)
    {
        if (c.Logic is not null && c.Conditions is { Count: > 0 } children)
        {
            var inner = string.Join(", ", children.Select(DescribeCondition));
            return $"{c.Logic}({inner})";
        }
        if (!string.IsNullOrEmpty(c.Expression))
        {
            return c.Expression;
        }
        var op = c.Operator == "notEquals" ? "!=" : "==";
        return $"{c.State} {op} {c.Value}";
    }

    private static string DescribeAction(NuiAction a)
    {
        if (a.Arguments.Count == 0)
            return $"DO {a.Target}.{a.Name}";

        var args = string.Join(" ", a.Arguments.Select(kv => $"{kv.Key}={kv.Value}"));
        return $"DO {a.Target}.{a.Name} {args}";
    }

    // ---- parsing ------------------------------------------------------------

    public static bool TryParse(string text, out NuiBehavior? behavior,
        out string? componentId, out string? eventName, out string? error)
    {
        error = null;
        behavior = null;
        componentId = null;
        eventName = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "empty text";
            return false;
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        // ---- WHEN ----------------------------------------------------------
        var whenIdx = lines.FindIndex(l => l.StartsWith("WHEN ", StringComparison.Ordinal));
        if (whenIdx < 0)
        {
            error = "missing WHEN line";
            return false;
        }
        var whenParts = lines[whenIdx]["WHEN ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (whenParts.Length < 2)
        {
            error = "WHEN line must be: WHEN componentId eventName";
            return false;
        }
        componentId = whenParts[0];
        eventName = whenParts[1];

        // ---- IF (optional, may span multiple lines) -----------------------
        var ifIdx = lines.FindIndex(l => l.StartsWith("IF ", StringComparison.Ordinal));
        NuiCondition? condition = null;
        if (ifIdx >= 0)
        {
            // Collect the full condition text: the IF line plus any
            // continuation lines until the next WHEN or DO directive.
            var condParts = new List<string> { lines[ifIdx]["IF ".Length..] };
            int next = ifIdx + 1;
            while (next < lines.Count &&
                   !lines[next].StartsWith("DO ", StringComparison.Ordinal) &&
                   !lines[next].StartsWith("WHEN ", StringComparison.Ordinal))
            {
                condParts.Add(lines[next]);
                next++;
            }
            var condText = string.Join(" ", condParts).Trim();
            try
            {
                condition = ParseCondition(condText);
            }
            catch (ParseException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ---- DO (one or more) ---------------------------------------------
        var doLines = lines.Where(l => l.StartsWith("DO ", StringComparison.Ordinal)).ToList();
        if (doLines.Count == 0)
        {
            error = "at least one DO line is required";
            return false;
        }

        var actions = new List<NuiAction>();
        foreach (var doLine in doLines)
        {
            try
            {
                actions.Add(ParseAction(doLine));
            }
            catch (ParseException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        behavior = new NuiBehavior
        {
            Id = string.Empty, // caller assigns
            Condition = condition,
            Action = actions.Count == 1 ? actions[0] : null,
            Actions = actions.Count > 1 ? actions : null,
        };

        return true;
    }

    // ---- condition parsing --------------------------------------------------

    private static NuiCondition ParseCondition(string text)
    {
        text = text.Trim();

        // Group: and(...) / or(...)
        if (text.StartsWith("and(", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("or(", StringComparison.OrdinalIgnoreCase))
        {
            return ParseGroup(text);
        }

        // Legacy equality: state op value  (op = == or !=)
        // Try to split on first == or != — but expression can contain == too.
        // Distinguishing heuristic: if the text contains a space AND
        // == or !=, treat as equality. If no space (or no op), treat as expression.
        if (TrySplitEquality(text, out var state, out var op, out var value))
        {
            return new NuiCondition
            {
                State = state,
                Operator = op,
                Value = ParseValue(value),
            };
        }

        // Expression
        if (!string.IsNullOrWhiteSpace(text) && text != "true" && text != "false")
        {
            return new NuiCondition { Expression = text };
        }

        throw new ParseException($"cannot parse condition: '{text}'");
    }

    private static NuiCondition ParseGroup(string text)
    {
        string logic;
        string inner;
        if (text.StartsWith("and(", StringComparison.OrdinalIgnoreCase))
        {
            logic = "and";
            inner = text["and(".Length..];
        }
        else
        {
            logic = "or";
            inner = text["or(".Length..];
        }

        if (!inner.EndsWith(')'))
        {
            throw new ParseException($"group must end with ')': '{text}'");
        }
        inner = inner[..^1].Trim();

        // Split children at top-level commas (not inside nested parens)
        var children = SplitTopLevel(inner).Select(ParseCondition).ToList();

        return new NuiCondition
        {
            Logic = logic,
            Conditions = children,
        };
    }

    private static List<string> SplitTopLevel(string text)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(') depth++;
            else if (text[i] == ')') depth--;
            else if (text[i] == ',' && depth == 0)
            {
                result.Add(text[start..i].Trim());
                start = i + 1;
            }
        }
        if (start < text.Length)
        {
            var last = text[start..].Trim();
            if (last.Length > 0) result.Add(last);
        }
        return result;
    }

    private static bool TrySplitEquality(string text, out string state, out string op, out string value)
    {
        state = string.Empty;
        op = string.Empty;
        value = string.Empty;

        // Find last == or != in the text (not inside strings)
        int eqIdx = text.LastIndexOf("==", StringComparison.Ordinal);
        int neIdx = text.LastIndexOf("!=", StringComparison.Ordinal);
        int idx = Math.Max(eqIdx, neIdx);
        if (idx <= 0) return false;

        op = text.Substring(idx, 2) == "!=" ? "notEquals" : "equals";
        state = text[..idx].Trim();
        value = text[(idx + 2)..].Trim();

        // Reject if either side looks like an expression (contains parens, logical operators, etc.)
        if (state.Contains('(') || state.Contains(')') ||
            state.Contains("&&") || state.Contains("||"))
            return false;
        if (value.Contains('(') || value.Contains(')') ||
            value.Contains("&&") || value.Contains("||"))
            return false; // expression with ==, not a simple equality

        return !string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(value);
    }

    private static object? ParseValue(string text)
    {
        text = text.Trim();
        if (text == "true") return true;
        if (text == "false") return false;
        if (double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var num))
            return num;
        // Strip quotes if present
        if (text.StartsWith('"') && text.EndsWith('"') && text.Length >= 2)
            return text[1..^1];
        return text;
    }

    // ---- action parsing -----------------------------------------------------

    private static NuiAction ParseAction(string text)
    {
        text = text.Trim();
        if (!text.StartsWith("DO ", StringComparison.Ordinal))
        {
            throw new ParseException("action line must start with DO");
        }

        var body = text["DO ".Length..].Trim();
        // Split: first token is target.action, rest are key=value pairs
        var tokens = body.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new ParseException("DO line must include target.actionName");
        }

        var targetAction = tokens[0].Split('.', 2);
        if (targetAction.Length < 2 || string.IsNullOrWhiteSpace(targetAction[0]) || string.IsNullOrWhiteSpace(targetAction[1]))
        {
            throw new ParseException($"action must be target.actionName: '{tokens[0]}'");
        }

        var action = new NuiAction
        {
            Target = targetAction[0],
            Name = targetAction[1],
        };

        for (int i = 1; i < tokens.Length; i++)
        {
            var kv = tokens[i].Split('=', 2);
            if (kv.Length != 2 || string.IsNullOrWhiteSpace(kv[0]))
            {
                throw new ParseException($"argument must be key=value: '{tokens[i]}'");
            }
            action.Arguments[kv[0]] = ParseValue(kv[1]);
        }

        return action;
    }

    private sealed class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }
}
