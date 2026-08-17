using System.Text.Json.Serialization;

namespace Nyforge.Core.Nui;

/// <summary>
/// A single node in an NUI component tree — a Button, a Container, a
/// Window, anything that can appear on the design canvas. See
/// docs/reference/nui-schema/NUI-SCHEMA.md §3.
/// </summary>
public sealed class NuiComponent
{
    /// <summary>Stable identifier, unique within the containing document.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Must match an entry in NuiComponentVocabulary. Forge's palette must
    /// never offer a type absent from that list (NFC-001 §4.3).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; } = new();

    [JsonPropertyName("layout")]
    public NuiLayout Layout { get; set; } = new();

    /// <summary>
    /// Event name -> behavior id. Null means unbound. See NuiBehavior and
    /// docs/reference/nui-schema/NUI-SCHEMA.md §7 (added in schema v0.2.0).
    /// </summary>
    [JsonPropertyName("events")]
    public Dictionary<string, string?> Events { get; set; } = new();

    [JsonPropertyName("children")]
    public List<NuiComponent> Children { get; set; } = new();

    /// <summary>
    /// When set, this node is an *instance* of a reusable component
    /// defined in the document's <c>components[]</c> section (NFS-006's
    /// reusable-component story): the node stores a reference to the
    /// master by name plus per-instance property overrides, instead of
    /// copying the master's tree. Null = a plain authored component.
    /// See ReusableComponentResolver.
    /// </summary>
    [JsonPropertyName("componentRef")]
    public string? ComponentRef { get; set; }

    /// <summary>Per-instance property overrides, applied on top of the
    /// referenced master's properties. Only meaningful when
    /// <see cref="ComponentRef"/> is set.</summary>
    [JsonPropertyName("overrides")]
    public Dictionary<string, object?> Overrides { get; set; } = new();

    public NuiComponent Clone()
    {
        return new NuiComponent
        {
            Id = Id,
            Type = Type,
            Properties = new Dictionary<string, object?>(Properties),
            Layout = Layout.Clone(),
            Events = new Dictionary<string, string?>(Events),
            Children = Children.Select(c => c.Clone()).ToList(),
            ComponentRef = ComponentRef,
            Overrides = new Dictionary<string, object?>(Overrides)
        };
    }
}

/// <summary>Design-time position and size on the canvas.</summary>
public sealed class NuiLayout
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; } = 100;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 32;

    public NuiLayout Clone() => new() { X = X, Y = Y, Width = Width, Height = Height };
}
