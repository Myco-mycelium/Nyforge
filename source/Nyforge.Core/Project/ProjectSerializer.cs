using System.Text.Json;
using Nyforge.Core.Nui;

namespace Nyforge.Core.Project;

/// <summary>
/// Reads and writes .nstudio files. This is the concrete implementation of
/// "save the project as its own code" — a .nstudio file is the serialized
/// NuiDocument, nothing more, nothing Forge-only. See
/// docs/how-to/saving-and-loading-projects.md.
/// </summary>
public static class ProjectSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ObjectToInferredTypesConverter() },
    };

    public static string Serialize(NuiDocument document)
    {
        document.Version = NuiSchemaVersion.Current;
        document.Project.Updated = DateTimeOffset.UtcNow;
        return JsonSerializer.Serialize(document, Options);
    }

    public static void SaveToFile(NuiDocument document, string path)
    {
        var json = Serialize(document);
        File.WriteAllText(path, json);
    }

    public static NuiDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<NuiDocument>(json, Options)
            ?? throw new InvalidDataException("File did not contain a valid NUI document.");

        if (!IsCompatible(document.Version))
        {
            throw new NuiVersionMismatchException(document.Version, NuiSchemaVersion.Current);
        }

        return document;
    }

    public static NuiDocument LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return Deserialize(json);
    }

    /// <summary>
    /// v0.1 compatibility rule: same MAJOR.MINOR is compatible, PATCH is
    /// always compatible. Once the schema reaches 1.0.0 this follows the
    /// full guarantee in NFC-001 §4.2; while it's 0.x, MINOR bumps may be
    /// breaking per semver convention, so we're deliberately strict here.
    /// </summary>
    private static bool IsCompatible(string documentVersion)
    {
        var docParts = documentVersion.Split('.');
        var curParts = NuiSchemaVersion.Current.Split('.');
        if (docParts.Length < 2 || curParts.Length < 2) return false;
        return docParts[0] == curParts[0] && docParts[1] == curParts[1];
    }
}

public sealed class NuiVersionMismatchException : Exception
{
    public string DocumentVersion { get; }
    public string RuntimeVersion { get; }

    public NuiVersionMismatchException(string documentVersion, string runtimeVersion)
        : base($"This .nstudio file was written against NUI schema {documentVersion}, " +
               $"but this build of Nyforge understands {runtimeVersion}. Open it with a " +
               $"matching Nyforge version, or migrate the file.")
    {
        DocumentVersion = documentVersion;
        RuntimeVersion = runtimeVersion;
    }
}
