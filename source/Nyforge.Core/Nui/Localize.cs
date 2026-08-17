namespace Nyforge.Core.Nui;

/// <summary>
/// Resolution of <c>$localize:key</c> references (NUI-SCHEMA §8.1):
/// a string value like <c>"$localize:settings.save"</c> resolves through
/// the active locale's table to its localized text. Plain text with no
/// references is returned unchanged; a missing key stays as the literal
/// placeholder (fail-soft at resolution — the validator rejects missing
/// keys up front, mirroring the Nyrqis import gate).
/// </summary>
public static class Localize
{
    public const string Prefix = "$localize:";

    /// <summary>Whether the string carries a <c>$localize:</c> reference.</summary>
    public static bool HasReference(string text) => text.Contains(Prefix, StringComparison.Ordinal);

    /// <summary>Resolve every <c>$localize:key</c> reference in
    /// <paramref name="text"/> through <paramref name="table"/> (the
    /// active locale's table). Keys absent from the table are left as
    /// the literal placeholder.</summary>
    public static string Resolve(string text, IReadOnlyDictionary<string, string>? table)
    {
        if (!HasReference(text) || table is null || table.Count == 0)
        {
            return text;
        }

        var result = text;
        foreach (var (key, value) in table)
        {
            result = result.Replace(Prefix + key, value, StringComparison.Ordinal);
        }
        return result;
    }

    /// <summary>Resolve through the document's active locale table.</summary>
    public static string Resolve(string text, NuiDocument document)
    {
        var locales = document.Locales;
        var table = locales.Tables.TryGetValue(locales.Active, out var active)
            ? active
            : null;
        return Resolve(text, table);
    }

    /// <summary>Every <c>$localize:key</c> reference in the string, in
    /// order of appearance (keys are [A-Za-z0-9_.-]+).</summary>
    public static IEnumerable<string> References(string text)
    {
        if (!HasReference(text)) yield break;
        var rest = text;
        while (true)
        {
            var pos = rest.IndexOf(Prefix, StringComparison.Ordinal);
            if (pos < 0) yield break;
            rest = rest[(pos + Prefix.Length)..];
            var key = new string(rest.TakeWhile(c =>
                char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-').ToArray());
            if (key.Length > 0) yield return key;
        }
    }
}
