namespace Nyforge.Core.Nui;

/// <summary>
/// Pure tree operations over NuiComponent hierarchies — the shared,
/// testable core behind nested-tree canvas editing (v0.6, Priority 1 of
/// the 2026-08-17 architecture review). No editor state lives here, per
/// NFC-001 §5.1: the Shell drives these with its own ViewModels. Layout
/// coordinates are *relative to the parent* (see NUI-SCHEMA.md §3 and
/// examples/settings-app/settings-app.nstudio), so every operation that
/// moves a node across parents must translate its Layout by the parent
/// offset delta to preserve the visual position.
/// </summary>
public static class ComponentTree
{
    /// <summary>
    /// Types that can hold child components, per NUI-SCHEMA.md §3 ("Container,
    /// Stack, Grid, Split View, etc. hold child Component nodes"). Mirrors the
    /// Layout category plus Window/Dialog from the v0.1 vocabulary (§4).
    /// </summary>
    public static readonly HashSet<string> ContainerTypes = new(StringComparer.Ordinal)
    {
        "Container", "Stack", "Grid", "FlexLayout", "SplitView", "ScrollView",
        "Card", "Panel", "Toolbar", "StatusBar",
        "Window", "Dialog"
    };

    public static bool CanContainChildren(string type) => ContainerTypes.Contains(type);

    /// <summary>Depth-first search for a node by id. Returns null when absent.</summary>
    public static NuiComponent? Find(NuiComponent root, string id)
    {
        if (root.Id == id) return root;
        foreach (var child in root.Children)
        {
            var found = Find(child, id);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>
    /// The parent and child index of the node with the given id. Returns
    /// (null, -1) when the node is not in the tree (or is the root itself).
    /// </summary>
    public static (NuiComponent? parent, int index) FindParentAndIndex(NuiComponent root, string id)
    {
        for (var i = 0; i < root.Children.Count; i++)
        {
            var child = root.Children[i];
            if (child.Id == id) return (root, i);
            var (parent, index) = FindParentAndIndex(child, id);
            if (parent is not null) return (parent, index);
        }
        return (null, -1);
    }

    /// <summary>
    /// Removes the node with the given id from wherever it sits in the
    /// tree (its children travel with it). Returns the removed node, or
    /// null when it isn't in the tree.
    /// </summary>
    public static NuiComponent? Remove(NuiComponent root, string id)
    {
        var (parent, index) = FindParentAndIndex(root, id);
        if (parent is null || index < 0) return null;
        var node = parent.Children[index];
        parent.Children.RemoveAt(index);
        return node;
    }

    /// <summary>
    /// Appends (or inserts at <paramref name="index"/>) a child into a
    /// container. Returns false — without mutating anything — when the
    /// parent type can't hold children.
    /// </summary>
    public static bool Insert(NuiComponent parent, NuiComponent child, int index = -1)
    {
        if (!CanContainChildren(parent.Type)) return false;
        if (index < 0 || index > parent.Children.Count) index = parent.Children.Count;
        parent.Children.Insert(index, child);
        return true;
    }

    /// <summary>Depth-first, parents before children — document/z-order order.</summary>
    public static IEnumerable<NuiComponent> Walk(NuiComponent root)
    {
        yield return root;
        foreach (var child in root.Children)
        {
            foreach (var descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    public static int Count(NuiComponent root) => Walk(root).Count();

    /// <summary>
    /// A node's canvas position: the sum of the relative Layout offsets up
    /// the parent chain. (root, root) is (0, 0).
    /// </summary>
    public static (double X, double Y) AbsolutePosition(NuiComponent root, NuiComponent node)
    {
        (double X, double Y)? Recurse(NuiComponent current, double accX, double accY)
        {
            if (current == node) return (accX, accY);
            foreach (var child in current.Children)
            {
                var found = Recurse(child, accX + child.Layout.X, accY + child.Layout.Y);
                if (found is not null) return found;
            }
            return null;
        }

        return Recurse(root, 0, 0) ?? (0, 0);
    }

    /// <summary>
    /// Moves <paramref name="node"/> under <paramref name="newParent"/>,
    /// adjusting its relative Layout so its absolute canvas position is
    /// unchanged. Returns false — without mutating anything — when the
    /// move is impossible: same node, non-container target, target inside
    /// the node's own subtree, or the node not present in the tree.
    /// </summary>
    public static bool Reparent(NuiComponent root, NuiComponent node, NuiComponent newParent)
    {
        if (node == newParent) return false;
        if (!CanContainChildren(newParent.Type)) return false;
        if (Find(node, newParent.Id) is not null) return false; // would create a cycle

        var (absX, absY) = AbsolutePosition(root, node);
        var (parX, parY) = AbsolutePosition(root, newParent);

        var removed = Remove(root, node.Id);
        if (removed is null || removed != node) return false;

        node.Layout.X = absX - parX;
        node.Layout.Y = absY - parY;
        Insert(newParent, node);
        return true;
    }
}
