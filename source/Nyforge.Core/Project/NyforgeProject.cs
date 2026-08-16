using Nyforge.Core.Nui;

namespace Nyforge.Core.Project;

/// <summary>
/// Wraps a NuiDocument with the bookkeeping an editor needs (file path,
/// unsaved-changes flag) without polluting the document model itself with
/// editor concerns — keeps NuiDocument a clean, portable artifact.
/// </summary>
public sealed class NyforgeProject
{
    public NuiDocument Document { get; private set; }
    public string? FilePath { get; private set; }
    public bool IsDirty { get; private set; }

    public NyforgeProject(NuiDocument? document = null)
    {
        Document = document ?? CreateBlank();
    }

    public static NuiDocument CreateBlank()
    {
        var doc = new NuiDocument();
        doc.Screens.Add(new NuiScreen
        {
            Id = "main",
            Root = new NuiComponent { Id = "window_main", Type = "Window", Layout = new NuiLayout { Width = 1024, Height = 768 } }
        });
        return doc;
    }

    public void MarkDirty() => IsDirty = true;

    public void Save(string? path = null)
    {
        var target = path ?? FilePath
            ?? throw new InvalidOperationException("No file path set; use Save As.");
        ProjectSerializer.SaveToFile(Document, target);
        FilePath = target;
        IsDirty = false;
    }

    public static NyforgeProject Load(string path)
    {
        var document = ProjectSerializer.LoadFromFile(path);
        // Private setters on FilePath/IsDirty are accessible here because
        // Load is a static member of this same class.
        return new NyforgeProject(document) { FilePath = path, IsDirty = false };
    }
}
