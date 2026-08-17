using System.Text.Json;
using System.Text.Json.Serialization;
using Nyforge.Core.Nui;

namespace Nyforge.Core.Project;

/// <summary>
/// Copy/paste payloads for the component clipboard (v0.6). A payload is a
/// JSON array of component subtrees, serialized with the same options as
/// .nstudio files so property values round-trip as native types. Paste
/// clones with fresh ids — component ids must stay unique within the
/// document — and drops the Events maps: behaviors are document-scoped
/// (document.Behaviors) and cannot cross a clipboard, so pasted
/// components arrive unbound rather than pointing at behavior ids that
/// don't exist.
/// </summary>
public static class ComponentClipboard
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ObjectToInferredTypesConverter() },
    };

    public static string Serialize(IEnumerable<NuiComponent> components)
        => JsonSerializer.Serialize(components.ToList(), Options);

    /// <summary>Parses a payload; returns an empty list for garbage rather than throwing.</summary>
    public static List<NuiComponent> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<NuiComponent>>(json, Options) ?? new List<NuiComponent>();
        }
        catch (JsonException)
        {
            return new List<NuiComponent>();
        }
    }

    /// <summary>
    /// Deep-clones each copied subtree with a fresh id per node (same
    /// scheme the palette's add uses: <c>type_abcdef</c>) and empty Events
    /// maps, preserving type, properties, layout, and child structure.
    /// </summary>
    public static List<NuiComponent> CloneWithFreshIds(IEnumerable<NuiComponent> components)
        => components.Select(CloneNode).ToList();

    private static NuiComponent CloneNode(NuiComponent source)
    {
        return new NuiComponent
        {
            Id = $"{source.Type.ToLowerInvariant()}_{Guid.NewGuid().ToString("N")[..6]}",
            Type = source.Type,
            Properties = new Dictionary<string, object?>(source.Properties),
            Layout = source.Layout.Clone(),
            Events = new Dictionary<string, string?>(), // stripped — behaviors are document-scoped
            Children = source.Children.Select(CloneNode).ToList()
        };
    }
}
