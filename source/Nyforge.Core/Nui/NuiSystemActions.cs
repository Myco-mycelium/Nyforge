namespace Nyforge.Core.Nui;

/// <summary>
/// An action not scoped to a specific component instance — the
/// "Nyrqis.*" calls from the original design doc.  Auto-generated from
/// the Nyrqis API Registry (engineering/registry/nui-api-v1.json) by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record SystemActionContract(
    string Name,
    IReadOnlyList<string> ArgumentNames);

public static class NuiSystemActions
{

    public static readonly IReadOnlyList<SystemActionContract> All = new[]
    {
        new SystemActionContract("Nyrqis.Theme.Set", new[] { "theme" }),
        new SystemActionContract("Nyrqis.Settings.Commit", Array.Empty<string>()),
        new SystemActionContract("Nyrqis.Window.Close", new[] { "windowId" }),
        new SystemActionContract("Nyrqis.Dialog.Open", new[] { "dialogId" }),
        new SystemActionContract("Nyrqis.Dialog.Close", new[] { "dialogId" }),
        new SystemActionContract("Nyrqis.Notification.Show", new[] { "title", "message", "severity" }),
    };

    private static readonly Dictionary<string, SystemActionContract> ByName =
        All.ToDictionary(a => a.Name, StringComparer.Ordinal);

    public static bool TryGet(string name, out SystemActionContract? contract) =>
        ByName.TryGetValue(name, out contract);
}
