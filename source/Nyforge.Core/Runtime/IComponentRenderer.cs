using Nyforge.Core.Nui;

namespace Nyforge.Core.Runtime;

/// <summary>
/// A platform-specific renderer for a NUI component type. Implementations
/// live in the platform layer (Forge's Avalonia renderers, Nyrqis's own
/// runtime renderers, or a headless test renderer). The core never
/// references the concrete renderer — it only holds this interface.
/// </summary>
public interface IComponentRenderer
{
    /// <summary>
    /// One or more component type names this renderer handles
    /// (e.g. "Button", "Toggle", "Text"). The registry maps
    /// each name to the renderer.
    /// </summary>
    IReadOnlyList<string> SupportedTypes { get; }

    /// <summary>
    /// A human-readable label for diagnostics / the runtime inspector.
    /// </summary>
    string Name { get; }
}

/// <summary>
/// A renderer that also provides a default property map — useful
/// for renderers that know which properties to populate when a
/// component has no explicit overrides (e.g. a Button's default
/// label text comes from the "text" property).
/// </summary>
public interface IPropertyRenderer : IComponentRenderer
{
    /// <summary>
    /// Returns the default property values for this component type
    /// when the component's property bag is missing them. Called
    /// once per component during instantiation.
    /// </summary>
    IReadOnlyDictionary<string, object?> DefaultProperties(NuiComponent component);
}

/// <summary>
/// A renderer that can handle a component's layout before it is
/// placed on a canvas (used by the Preview and the Nyrqis runtime).
/// </summary>
public interface ILayoutRenderer : IComponentRenderer
{
    /// <summary>
    /// Whether this renderer acts as a layout container — i.e. its
    /// children should be positioned relative to it. Containers
    /// (Window, Container, Stack, Grid, etc.) return true.
    /// </summary>
    bool IsContainer { get; }
}

/// <summary>
/// A renderer that can participate in event binding — e.g. a Button
/// renderer knows it fires "clicked" and "pressed"/"released", so
/// the registry can validate that a behavior's WHEN clause references
/// an event the renderer actually supports.
/// </summary>
public interface IEventRenderer : IComponentRenderer
{
    /// <summary>
    /// The event names this renderer supports for WHEN clauses
    /// (e.g. "clicked", "changed", "opened").
    /// </summary>
    IReadOnlyList<string> SupportedEvents { get; }
}

/// <summary>
/// Registry of component renderers. The Forge shell and Nyrqis runtime
/// each register their own renderers; the core tests use mock renderers.
/// </summary>
public sealed class ComponentRendererRegistry
{
    private readonly Dictionary<string, IComponentRenderer> _byType = new(StringComparer.Ordinal);
    private readonly List<IComponentRenderer> _all = new();

    /// <summary>
    /// Total number of registered renderers (each may handle multiple types).
    /// </summary>
    public int RendererCount => _all.Count;

    /// <summary>
    /// Total number of registered type mappings (some renderers handle multiple types).
    /// </summary>
    public int TypeCount => _byType.Count;

    /// <summary>
    /// Register a renderer. If any of its SupportedTypes are already registered,
    /// the new renderer silently wins (last-write-wins, matching the component
    /// contract override pattern).
    /// </summary>
    public void Register(IComponentRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        foreach (var type in renderer.SupportedTypes)
        {
            _byType[type] = renderer;
        }
        _all.Add(renderer);
    }

    /// <summary>
    /// Returns the renderer for a component type, or null if none is registered.
    /// </summary>
    public IComponentRenderer? GetRenderer(string componentType)
    {
        return _byType.TryGetValue(componentType, out var r) ? r : null;
    }

    /// <summary>
    /// Returns true if a renderer is registered for the component type.
    /// </summary>
    public bool HasRenderer(string componentType) => _byType.ContainsKey(componentType);

    /// <summary>
    /// Returns all registered renderers.
    /// </summary>
    public IReadOnlyList<IComponentRenderer> GetAll() => _all.AsReadOnly();
}
