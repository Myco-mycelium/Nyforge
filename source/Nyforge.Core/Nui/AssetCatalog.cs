using System.Security.Cryptography;

namespace Nyforge.Core.Nui;

/// <summary>
/// The asset catalog (NUI-SCHEMA §8.2): hashing and deduplication for a
/// document's managed resources. Content hashes make dedup and packaging
/// deterministic — two files with the same sha256 are one asset — and
/// let the validator flag duplicate content instead of silent bloat.
/// </summary>
public static class AssetCatalog
{
    /// <summary>Compute the sha256 content hash of a file (lowercase hex).</summary>
    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Register a file as an asset. When an existing asset already has
    /// the same content hash, the new entry is returned as a duplicate
    /// (<paramref name="isDuplicate"/> = true) — callers can coalesce it
    /// to the existing asset rather than shipping the bytes twice.
    /// </summary>
    public static NuiAsset RegisterFromFile(
        string id,
        NuiAssetKind kind,
        string relativePath,
        string absolutePath,
        IReadOnlyList<NuiAsset> existing,
        out bool isDuplicate)
    {
        var sha = ComputeSha256(absolutePath);
        isDuplicate = existing.Any(a => a.Sha256 == sha);
        return new NuiAsset
        {
            Id = id,
            Kind = kind.ToString().ToLowerInvariant(),
            Path = relativePath,
            Sha256 = sha,
        };
    }

    /// <summary>All declared asset ids, for reference checks.</summary>
    public static HashSet<string> AssetIds(NuiDocument document) =>
        new(document.Resources.Assets.Select(a => a.Id), StringComparer.Ordinal);
}
