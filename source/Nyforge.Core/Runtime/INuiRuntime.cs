using Nyforge.Core.Nui;

namespace Nyforge.Core.Runtime;

/// <summary>
/// The interface every NUI runtime must implement — the seam between
/// the editor (Nyforge) and the OS (Nyrqis). Both the Forge preview
/// stand-in and the real Nyrqis UI Runtime implement this so
/// application logic (behaviors, bindings, state) is identical
/// everywhere (NFC-001 §5.1 / doc #8/#9).
///
/// A runtime owns:
/// - **Runtime state** — the live key/value store that expressions and
///   bindings resolve against (mirrors the floor's <c>resolve_states</c>).
/// - **Event dispatch** — looking up the behavior bound to a component's
///   event, evaluating its condition, and executing its action(s).
/// - **Binding application** — syncing a state value into a component property.
/// - **An event log** — for diagnostics and debugging.
///
/// The Forge preview (<see cref="ForgePreviewRuntime"/>) is the honest
/// stand-in that runs today. A future <c>NyrqisRuntime</c> will provide
/// the real shell rendering on the actual OS — same interface, different
/// host-specific rendering.
/// </summary>
public interface INuiRuntime
{
    /// <summary>The live runtime state dictionary (flat keys + scoped
    /// dotted names, NUI-SCHEMA §8.4). The runtime reads and writes
    /// this; expressions and bindings resolve against it.</summary>
    IDictionary<string, object?> RuntimeStates { get; }

    /// <summary>Fires a component's event: looks up the behavior
    /// bound to <paramref name="eventName"/> on
    /// <paramref name="component"/>, evaluates its condition against
    /// <see cref="RuntimeStates"/>, and executes its action(s) if the
    /// condition holds.</summary>
    void FireEvent(NuiComponent component, string eventName);

    /// <summary>Applies a binding: reads the state value from
    /// <see cref="RuntimeStates"/> and writes it into the runtime's
    /// rendering of the bound component's property. The concrete
    /// implementation decides what "writes into the rendering" means
    /// (Forge updates a preview VM; the real runtime updates the
    /// actual shell widget).</summary>
    void ApplyBinding(NuiBinding binding);

    /// <summary>Diagnostic event log (most recent first). Every
    /// significant runtime action appends a human-readable message.</summary>
    IList<string> Log { get; }
}
