using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nyforge.Core.Nui;

/// <summary>
/// What a schema migration did to a document, if anything.
/// <see cref="Applied"/> lists each step as "0.2.0 -> 0.3.0" so callers
/// can tell the user exactly what changed (and so tests can pin the
/// chain). Empty <see cref="Applied"/> = the document was already
/// current or future-versioned (the latter is the serializer's
/// mismatch error, not a migration concern).
/// </summary>
public sealed record NuiMigrationResult(
    string FromVersion,
    string ToVersion,
    IReadOnlyList<string> Applied,
    string MigratedJson);

/// <summary>
/// The NUI schema migration chain — "old .nstudio files must continue to
/// open" (NFC-001 §4.2). Migrations operate on the raw JSON *before* the
/// document is parsed, so a step can restructure anything; each step is
/// a small, deterministic transform from one schema version to the next.
///
/// Rules that make this safe:
/// - Migrations are **in-memory only** — the input string is never
///   mutated, and the file on disk is never touched until the user saves.
/// - A document is only ever moved *forward* toward
///   <see cref="NuiSchemaVersion.Current"/>, one step at a time, in
///   version order; a document at the current version is never touched.
/// - Every step must be idempotent in spirit: if the target section
///   already exists, the step leaves it alone.
/// - Future-versioned documents (a version this build doesn't know) are
///   returned untouched — the serializer's version-mismatch error owns
///   that case, not a speculative downgrade.
///
/// To add a migration for a future breaking change: bump
/// <see cref="NuiSchemaVersion.Current"/>, add one (From, To, transform)
/// step below, and pin it with a test that runs an old fixture through
/// the chain.
/// </summary>
public static class NuiSchemaMigrations
{
    /// <summary>
    /// One migration step: moves a document from <c>From</c> to <c>To</c>
    /// by transforming the raw JSON root in place. Must be a no-op when
    /// the target shape already exists.
    /// </summary>
    private sealed record Step(string From, string To, Action<JsonObject> Migrate);

    /// <summary>
    /// The ordered chain. Keep ascending and contiguous: each step's
    /// <c>From</c> must be the previous step's <c>To</c>.
    /// </summary>
    private static readonly Step[] Chain =
    {
        // 0.2.0 -> 0.3.0: the Bindings section arrived (NFS-003). Early
        // v0.2 documents were authored before it existed; canonical form
        // carries the section explicitly rather than relying on the
        // parser's default.
        new("0.2.0", "0.3.0", root =>
        {
            if (root["bindings"] is null)
                root["bindings"] = new JsonArray();
        }),
        // 0.3.0 -> 0.4.0: $state: expression-valued action arguments
        // (NFS-005) read document-level state; canonical form carries
        // the section explicitly.
        new("0.3.0", "0.4.0", root =>
        {
            if (root["states"] is null)
                root["states"] = new JsonObject();
        }),
    };

    /// <summary>
    /// Migrate <paramref name="json"/> forward to the current schema
    /// version if needed. Returns null when the document is already at
    /// the current version (or unparseable / future-versioned — those
    /// are the serializer's concerns, not a migration). Never mutates
    /// <paramref name="json"/>.
    /// </summary>
    public static NuiMigrationResult? MigrateIfNeeded(string json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null; // malformed JSON is the serializer's error to report
        }

        if (root is not JsonObject obj ||
            obj["version"] is not JsonValue versionValue ||
            versionValue.GetValue<string>() is not { } version)
        {
            return null;
        }

        var (major, minor, _) = ParseVersion(version);
        if (major is not int majorInt || minor is not int minorInt) return null;

        var (curMajor, curMinor, _) = ParseVersion(NuiSchemaVersion.Current);
        if (majorInt > curMajor || (majorInt == curMajor && minorInt >= curMinor))
            return null; // current or future

        var applied = new List<string>();
        foreach (var step in Chain)
        {
            var (stepFromMajor, stepFromMinor, _) = ParseVersion(step.From);
            if (majorInt != stepFromMajor || minorInt != stepFromMinor) continue;
            step.Migrate(obj);
            obj["version"] = step.To;
            applied.Add($"{step.From} -> {step.To}");
            var next = ParseVersion(step.To);
            majorInt = next.major ?? 0;
            minorInt = next.minor ?? 0;
        }

        if (applied.Count == 0) return null;

        return new NuiMigrationResult(
            version,
            NuiSchemaVersion.Current,
            applied,
            obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static (int? major, int? minor, int? patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length < 2) return (null, null, null);
        return (
            int.TryParse(parts[0], out var major) ? major : null,
            int.TryParse(parts[1], out var minor) ? minor : null,
            parts.Length >= 3 && int.TryParse(parts[2], out var patch) ? patch : null);
    }
}
