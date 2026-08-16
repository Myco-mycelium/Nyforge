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

/// <summary>Simple equality condition against a named document state value.</summary>
public sealed class NuiCondition
{
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals"; // equals | notEquals — v0.2 keeps this deliberately small

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    public NuiCondition Clone() => new() { State = State, Operator = Operator, Value = Value };
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
