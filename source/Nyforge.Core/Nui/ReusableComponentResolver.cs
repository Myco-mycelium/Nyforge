using Nyforge.Core.Nui;

namespace Nyforge.Core.Nui;

/// <summary>
/// Materializes reusable-component *instances* (NFS-006 / NUI-SCHEMA
/// §9's reserved <c>components[]</c>): a node with a <see cref="NuiComponent.ComponentRef"/>
/// is a reference to a master definition plus per-instance overrides —
/// not a copy of the master's tree.  Change the master once, every
/// instance reflects it (the editor can offer "push change to instances"
/// later; resolution itself is always from the live master).
///
/// Resolution rules:
/// 1. The instance's own <c>Properties</c> are the baseline (so an
///    instance can still carry authored properties even before overrides
///    land in the schema editor).
/// 2. The master's tree is cloned onto the instance node.
/// 3. The master's properties are then overlaid by the instance's
///    <c>Overrides</c> (and the instance's own properties, which win).
/// 4. The instance's authored <c>Children</c> are appended after the
///    master's children (a master may declare a slot order; append is
///    the v0.6 convention).
/// 5. The instance's own <c>Id</c>/<c>Type</c>/<c>Layout</c>/<c>Events</c>
///    are preserved (layout is the instance's design-time placement).
/// </summary>
public static class ReusableComponentResolver
{
    /// <summary>Whether the node is a reusable-component instance.</summary>
    public static bool IsInstance(NuiComponent node) =>
        !string.IsNullOrEmpty(node.ComponentRef);

    /// <summary>
    /// Resolve one instance against the document's masters.  Returns
    /// null when the node isn't an instance or the referenced master
    /// doesn't exist (the caller decides: canvas render can skip it,
    /// validation can report it).
    /// </summary>
    public static NuiComponent? Resolve(NuiComponent instance, NuiDocument document)
    {
        if (!IsInstance(instance)) return null;
        var master = FindMaster(document, instance.ComponentRef!);
        if (master is null) return null;

        // Clone the master's whole tree, then graft the instance's
        // authored data on top.
        var resolved = master.Clone();
        resolved.Id = instance.Id;
        resolved.Layout = instance.Layout.Clone();

        // Master properties + instance-authored properties, then the
        // instance's overrides win last.
        foreach (var (key, value) in instance.Properties) resolved.Properties[key] = value;
        foreach (var (key, value) in instance.Overrides) resolved.Properties[key] = value;

        foreach (var (name, behaviorId) in instance.Events) resolved.Events[name] = behaviorId;

        foreach (var child in instance.Children) resolved.Children.Add(child.Clone());
        return resolved;
    }

    /// <summary>Find a master by id in the document's reusable-components section.</summary>
    public static NuiComponent? FindMaster(NuiDocument document, string id) =>
        document.ReusableComponents.FirstOrDefault(m => m.Id == id);
}
