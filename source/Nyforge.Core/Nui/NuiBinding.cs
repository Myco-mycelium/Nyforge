using System.Text.Json.Serialization;

namespace Nyforge.Core.Nui;

/// <summary>
/// A property <-> state binding: keeps a component's property and a
/// document-level state value in sync. See NUI-SCHEMA.md §8.
///
/// v0.3 scope: one-directional-in-effect but bidirectional-in-practice —
/// the bound property both seeds from and writes back to the same state
/// key. Computed/derived bindings (an expression rather than a raw state
/// reference) are out of scope; see engineering/ROADMAP.md.
/// </summary>
public sealed class NuiBinding
{
    [JsonPropertyName("component")]
    public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    public NuiBinding Clone() => new() { ComponentId = ComponentId, Property = Property, State = State };
}
