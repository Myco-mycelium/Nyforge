using System.Text.Json.Serialization;

namespace Nyforge.Core.Nui;

/// <summary>
/// A single "WHEN [event] IF [condition] DO [action(s)]" rule, per the
/// original design document's logic model. A component's Events dict
/// (see NuiComponent) maps an event name to the Id of one of these.
///
/// A behavior declares exactly one of <see cref="Action"/> (single) or
/// <see cref="Actions"/> (a chain run in order — NUI-SCHEMA §7.3). The
/// condition is a leaf (expression or state/operator/value equality) or
/// an AND/OR logic group of conditions, recursively.
/// </summary>
public sealed class NuiBehavior
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Optional. Null means the actions always run when the event fires.</summary>
    [JsonPropertyName("condition")]
    public NuiCondition? Condition { get; set; }

    /// <summary>The single-action form; null when the behavior uses <see cref="Actions"/>.</summary>
    [JsonPropertyName("action")]
    public NuiAction? Action { get; set; }

    /// <summary>The action-chain form (NUI-SCHEMA §7.3); null when the
    /// behavior uses the single <see cref="Action"/>.</summary>
    [JsonPropertyName("actions")]
    public List<NuiAction>? Actions { get; set; }

    public NuiBehavior Clone() => new()
    {
        Id = Id,
        Condition = Condition?.Clone(),
        Action = Action?.Clone(),
        Actions = Actions?.Select(a => a.Clone()).ToList()
    };
}

/// <summary>
/// A condition. A leaf is either the legacy simple equality form (a
/// named document state value, <see cref="State"/>/<see cref="Operator"/>/<see cref="Value"/>) or — when
/// <see cref="Expression"/> is set — a full NUI expression (NUI-SCHEMA
/// §7.2), which supersedes the equality form. When <see cref="Logic"/>
/// is set, the condition is an AND/OR group (NUI-SCHEMA §7.3) whose
/// <see cref="Conditions"/> each validate and evaluate recursively — the
/// internal representation the visual logic-graph editor builds on.
/// </summary>
public sealed class NuiCondition
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals"; // equals | notEquals — v0.2 keeps this deliberately small

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// NUI expression (NUI-SCHEMA §7.2), e.g. <c>state.volume &gt; 50 &amp;&amp; !state.dnd</c>.
    /// When set, it supersedes the legacy equality fields.
    /// </summary>
    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    /// <summary>"and" or "or" when this condition is a logic group; null for a leaf.</summary>
    [JsonPropertyName("logic")]
    public string? Logic { get; set; }

    /// <summary>The group's sub-conditions when <see cref="Logic"/> is set;
    /// null for a leaf.</summary>
    [JsonPropertyName("conditions")]
    public List<NuiCondition>? Conditions { get; set; }

    public NuiCondition Clone() => new()
    {
        State = State,
        Operator = Operator,
        Value = Value,
        Expression = Expression,
        Logic = Logic,
        Conditions = Conditions?.Select(c => c.Clone()).ToList()
    };
}

/// <summary>
/// An action invocation: either a system-level Nyrqis API call
/// (target == "System", see NuiSystemActions) or a call against another
/// component on the same screen (target == that component's Id).
/// </summary>
public sealed class NuiAction
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = "System";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?> Arguments { get; set; } = new();

    public NuiAction Clone() => new()
    {
        Target = Target,
        Name = Name,
        Arguments = new Dictionary<string, object?>(Arguments)
    };
}
