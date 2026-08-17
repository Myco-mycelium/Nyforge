using System.Text.Json.Serialization;

namespace Nyforge.Core.Nui;

/// <summary>
/// A single "WHEN [event] IF [condition] DO [action]" rule, per the
/// original design document's logic model. A component's Events dict
/// (see NuiComponent) maps an event name to the Id of one of these.
///
/// v0.2 scope: one optional condition (a simple equality check against a
/// document-level state value), one action. Multi-condition boolean logic
/// and action chaining are v0.3+ — see engineering/ROADMAP.md.
/// </summary>
public sealed class NuiBehavior
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Optional. Null means the action always runs when the event fires.</summary>
    [JsonPropertyName("condition")]
    public NuiCondition? Condition { get; set; }

    [JsonPropertyName("action")]
    public NuiAction Action { get; set; } = new();

    public NuiBehavior Clone() => new()
    {
        Id = Id,
        Condition = Condition?.Clone(),
        Action = Action.Clone()
    };
}

/// <summary>
/// A condition. Either the legacy simple equality form (a named
/// document state value, <see cref="State"/>/<see cref="Operator"/>/<see cref="Value"/>) or — when
/// <see cref="Expression"/> is set — a full NUI expression (NUI-SCHEMA
/// §7.2), which supersedes the equality form. The same expression
/// string evaluates identically in NyForge, the reference floor, and
/// the Rust crate.
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

    public NuiCondition Clone() => new()
    {
        State = State,
        Operator = Operator,
        Value = Value,
        Expression = Expression
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
