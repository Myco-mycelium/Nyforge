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
    /// Localization (NUI-SCHEMA §8.1): the active locale plus per-locale
    /// string tables. A component property (or behavior argument) whose
    /// value is <c>$localize:key</c> resolves through the active locale's
    /// table. Empty section = no localization.
    /// </summary>
    [JsonPropertyName("locales")]
    public NuiLocalesSection Locales { get; set; } = new();

    /// <summary>
    /// Resources (NUI-SCHEMA §8.2): the project's managed asset catalog.
    /// A component property value like <c>$asset:wallpaper</c> names an
    /// asset by id; <see cref="AssetCatalog"/> computes the sha256
    /// content hash for deduplication and packaging.
    /// </summary>
    [JsonPropertyName("resources")]
    public NuiResourcesSection Resources { get; set; } = new();

    /// <summary>
    /// Animations (NUI-SCHEMA §8.3): the document's declarative
    /// animations. A behavior whose action is <c>Nyrqis.Animation.Play</c>
    /// references one by id (the system action's <c>animation</c>
    /// argument); the runtime plays the timed property transition on the
    /// animation's target component.
    /// </summary>
    [JsonPropertyName("animations")]
    public List<NuiAnimation> Animations { get; set; } = new();

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

/// <summary>
/// The localization section (NUI-SCHEMA §8.1): <see cref="Active"/> is
/// the current locale code and <see cref="Tables"/> maps locale codes
/// to string-key → localized-text tables. <c>$localize:key</c> string
/// values resolve through the active table (see Localize.Resolve).
/// </summary>
public sealed class NuiLocalesSection
{
    [JsonPropertyName("active")]
    public string Active { get; set; } = string.Empty;

    [JsonPropertyName("tables")]
    public Dictionary<string, Dictionary<string, string>> Tables { get; set; } = new();
}

/// <summary>Asset kinds (NUI-SCHEMA §8.2).</summary>
public enum NuiAssetKind
{
    Image,
    Svg,
    Icon,
    Font,
    Audio,
    Video,
    Material,
    Animation,
}

/// <summary>One managed resource: a stable id, a kind, its path relative
/// to the project, and the sha256 content hash (computed by
/// <see cref="AssetCatalog"/>, used for deduplication).</summary>
public sealed class NuiAsset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "image";

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }
}

/// <summary>The resources section (NUI-SCHEMA §8.2): the ordered asset
/// catalog. Ids are document-unique; <c>$asset:id</c> references resolve
/// against them.</summary>
public sealed class NuiResourcesSection
{
    [JsonPropertyName("assets")]
    public List<NuiAsset> Assets { get; set; } = new();
}

/// <summary>
/// One declarative animation (NUI-SCHEMA §8.3): a timed transition of
/// one of a target component's properties, triggered by a behavior's
/// <c>Nyrqis.Animation.Play</c> action. <see cref="Easing"/> is one of
/// linear / ease-in / ease-out / ease-in-out / steps; <see cref="Direction"/>
/// is one of forward / reverse / alternate. Keyframes (multi-point
/// transitions) are the documented follow-on.
/// </summary>
public sealed class NuiAnimation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; } = 300; // milliseconds

    [JsonPropertyName("delay")]
    public int Delay { get; set; }

    [JsonPropertyName("easing")]
    public string Easing { get; set; } = "ease-in-out";

    [JsonPropertyName("repeat")]
    public int Repeat { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "forward";
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
