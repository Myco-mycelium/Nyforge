using System.Text.Json.Serialization;

namespace Nyforge.Core.Nui;

/// <summary>
/// The root of a .nstudio project file. See
/// docs/reference/nui-schema/NUI-SCHEMA.md §2.
/// </summary>
public sealed class NuiDocument
{
    /// <summary>NUI schema version this document was written against.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = NuiSchemaVersion.Current;

    [JsonPropertyName("project")]
    public NuiProjectInfo Project { get; set; } = new();

    [JsonPropertyName("themes")]
    public NuiThemeSection Themes { get; set; } = new();

    [JsonPropertyName("screens")]
    public List<NuiScreen> Screens { get; set; } = new();

    // Reserved shape for v0.2 — see NUI-SCHEMA.md §9 (Non-Goals).
    [JsonPropertyName("components")]
    public List<NuiComponent> ReusableComponents { get; set; } = new();

    [JsonPropertyName("states")]
    public Dictionary<string, object?> States { get; set; } = new();

    /// <summary>
    /// v0.2: WHEN/IF/DO rules. A component's Events dict maps an event
    /// name to one of these by Id. See NuiBehavior and NUI-SCHEMA.md §7.
    /// </summary>
    [JsonPropertyName("behaviors")]
    public List<NuiBehavior> Behaviors { get; set; } = new();

    // Reserved shape — see NUI-SCHEMA.md §9. Full binding evaluation is v0.3+.
    [JsonPropertyName("bindings")]
    public List<NuiBinding> Bindings { get; set; } = new();
}

public sealed class NuiProjectInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Untitled Project";

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("updated")]
    public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class NuiScreen
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("root")]
    public NuiComponent Root { get; set; } = new() { Type = "Window", Id = "root" };

    [JsonPropertyName("size")]
    public NuiCanvasSize Size { get; set; } = new();
}

public sealed class NuiCanvasSize
{
    [JsonPropertyName("width")]
    public double Width { get; set; } = 1024;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 768;
}

public sealed class NuiThemeSection
{
    /// <summary>Active theme name, e.g. "Solar" or "Eclipse".</summary>
    [JsonPropertyName("active")]
    public string Active { get; set; } = "Eclipse";

    /// <summary>Per-project token overrides, applied on top of the active theme.</summary>
    [JsonPropertyName("overrides")]
    public Dictionary<string, string> Overrides { get; set; } = new();
}

public static class NuiSchemaVersion
{
    /// <summary>
    /// Must match docs/reference/nui-schema/NUI-SCHEMA.md front matter.
    /// Bump per NFC-001 §4.1/§8 when the schema changes, not when the app does.
    /// v0.4.0: added $state: expression-valued action arguments (NFS-005) — see NUI-SCHEMA.md §7.1.
    /// v0.3.0: added the Bindings section (NFS-003) — see NUI-SCHEMA.md §8.
    /// v0.2.0: added the Behaviors section (NFS-002) — see NUI-SCHEMA.md §7.
    /// </summary>
    public const string Current = "0.4.0";
}
