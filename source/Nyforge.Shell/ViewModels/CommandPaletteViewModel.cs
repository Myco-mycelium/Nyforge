using System.Collections.ObjectModel;
using System.Windows.Input;
using Nyforge.Core.Nui;
using Nyforge.Shell.Services;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// A single command entry in the Command Palette. Each entry has a label,
/// category, optional keyboard shortcut, and an action to execute.
/// </summary>
public sealed record PaletteCommand(
    string Label,
    string Category,
    string? Shortcut,
    string Icon,
    Action Execute,
    string? SearchTerms = null)
{
    public bool Matches(string query) =>
        string.IsNullOrEmpty(query) ||
        Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        (SearchTerms?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
}

/// <summary>
/// Cmd+K Command Palette ViewModel: provides a searchable list of all
/// editor commands organized by category. Apple-style command palette.
/// </summary>
public sealed class CommandPaletteViewModel : ViewModelBase
{
    private string _query = "";
    private bool _isOpen;

    public string Query
    {
        get => _query;
        set
        {
            SetField(ref _query, value);
            FilterCommands();
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            SetField(ref _isOpen, value);
            if (value) { Query = ""; FilterCommands(); }
        }
    }

    public ObservableCollection<PaletteCommand> AllCommands { get; } = new();
    public ObservableCollection<PaletteCommand> FilteredCommands { get; } = new();

    public PaletteCommand? SelectedCommand { get; set; }

    public event EventHandler? CommandExecuted;
    public event EventHandler? CloseRequested;

    public CommandPaletteViewModel(MainWindowViewModel mainVm, ProjectService projectService)
    {
        // --- File ---
        Add("New Project", "File", "Ctrl+N", "📄", () => mainVm.NewProject(),
            "new create blank empty");
        Add("Open Project", "File", "Ctrl+O", "📂", () => mainVm.HomeCommandRequestedFileDialog?.Invoke(mainVm, ForgeCommands.OpenProject),
            "open load file");
        Add("Save Project", "File", "Ctrl+S", "💾", () => mainVm.SaveToPath(),
            "save write export");
        Add("Save As...", "File", "Ctrl+Shift+S", "💾", () => mainVm.SaveToPath(null),
            "save export copy");

        // --- Edit ---
        Add("Undo", "Edit", "Ctrl+Z", "↩", () => mainVm.UndoCommand.Execute(null),
            "revert rollback");
        Add("Redo", "Edit", "Ctrl+Shift+Z", "↪", () => mainVm.RedoCommand.Execute(null),
            "reapply forward");
        Add("Copy Selection", "Edit", "Ctrl+C", "📋", () => mainVm.CopySelectionCommand.Execute(null),
            "copy clipboard");
        Add("Paste", "Edit", "Ctrl+V", "📋", () => mainVm.PasteCommand.Execute(null),
            "paste clipboard insert");
        Add("Delete Selected", "Edit", "Del", "🗑", () => mainVm.DeleteSelectedCommand.Execute(null),
            "delete remove erase");

        // --- View ---
        Add("Toggle Theme", "View", "Ctrl+Shift+T", "🌓", () => mainVm.SetThemeCommand.Execute(
            mainVm.CurrentTheme == "Eclipse" ? "Solar" : "Eclipse"),
            "theme dark light eclipse solar");

        // --- Project ---
        Add("Validate Document", "Project", null, "✅", () => { /* triggers validate */ },
            "validate check lint errors");
        Add("Preview", "Project", "F5", "▶", () => { /* triggers preview */ },
            "preview run display");
        Add("Run in Nyrqis", "Project", null, "🚀", () => { /* triggers run in nyrqis */ },
            "nyrqis runtime deploy");

        // --- Add Component (per category) ---
        foreach (var contract in ComponentContracts.All)
        {
            Add($"Add {contract.Type}", $"Add → {contract.Category}", null, "+",
                () => mainVm.AddComponentCommand.Execute(contract.Type),
                $"add insert new {contract.Type.ToLowerInvariant()} {contract.Category.ToLowerInvariant()}");
        }

        // --- Navigation ---
        Add("Go to Home", "Navigation", null, "🏠", () => { /* switch to Home tab */ },
            "home start tab");
        Add("Go to Design", "Navigation", null, "🎨", () => { /* switch to Design tab */ },
            "design canvas editor");
        Add("Go to Layers", "Navigation", null, "📑", () => { /* switch to Layers tab */ },
            "layers hierarchy tree");
        Add("Go to Events", "Navigation", null, "⚡", () => { /* switch to Events tab */ },
            "events behaviors logic");
        Add("Go to Animations", "Navigation", null, "🎬", () => { /* switch to Animations tab */ },
            "animations keyframes motion");

        // --- Help ---
        Add("About Nyforge", "Help", null, "ℹ", () => { /* opens about */ },
            "about version info credits");
        Add("Getting Started", "Help", null, "📖", () => { /* opens docs */ },
            "docs help tutorial guide");
        Add("NUI Schema Reference", "Help", null, "📖", () => { /* opens schema docs */ },
            "schema reference docs api");

        FilterCommands();
    }

    private void Add(string label, string category, string? shortcut, string icon,
                      Action execute, string? searchTerms = null)
    {
        AllCommands.Add(new PaletteCommand(label, category, shortcut, icon, execute, searchTerms));
    }

    private void FilterCommands()
    {
        FilteredCommands.Clear();
        foreach (var cmd in AllCommands.Where(c => c.Matches(_query)))
        {
            FilteredCommands.Add(cmd);
        }
        SelectedCommand = FilteredCommands.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedCommand));
    }

    public void ExecuteSelected()
    {
        if (SelectedCommand is null) return;
        SelectedCommand.Execute();
        IsOpen = false;
        CommandExecuted?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public void MoveSelection(int delta)
    {
        var idx = FilteredCommands.IndexOf(SelectedCommand!);
        var newIdx = Math.Clamp(idx + delta, 0, FilteredCommands.Count - 1);
        if (newIdx >= 0 && newIdx < FilteredCommands.Count)
        {
            SelectedCommand = FilteredCommands[newIdx];
            OnPropertyChanged(nameof(SelectedCommand));
        }
    }
}
