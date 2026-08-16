using Nyforge.Core.Project;

namespace Nyforge.Shell.Services;

/// <summary>
/// Thin editor-side wrapper around Nyforge.Core's project system. Keeps
/// file-dialog concerns (which live in Views, since they need a Window
/// reference) separate from the actual load/save logic.
/// </summary>
public sealed class ProjectService
{
    public NyforgeProject Current { get; private set; } = new(NyforgeProject.CreateBlank());

    public event EventHandler? ProjectChanged;

    public void NewProject()
    {
        Current = new NyforgeProject(NyforgeProject.CreateBlank());
        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Open(string path)
    {
        Current = NyforgeProject.Load(path);
        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save(string? path = null)
    {
        Current.Save(path);
        ProjectChanged?.Invoke(this, EventArgs.Empty);
    }
}
