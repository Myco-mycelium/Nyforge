using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Shell.Services;
using Nyforge.Shell.ViewModels;

namespace Nyforge.Shell.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) =>
        {
            if (Vm is not null)
            {
                Vm.HomeCommandRequestedFileDialog += OnHomeCommandRequestedFileDialog;
                Vm.CopyRequested += OnCopyRequested;
                Vm.PasteRequested += OnPasteRequested;
            }
        };
    }

    // --- Arrow-key nudging (4 px grid step; Shift = 5x) ---

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is null) return;
        if (FocusManager?.GetFocusedElement() is TextBox) return;

        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 20.0 : 4.0;
        switch (e.Key)
        {
            case Key.Left: Vm.Nudge(-step, 0); e.Handled = true; break;
            case Key.Right: Vm.Nudge(step, 0); e.Handled = true; break;
            case Key.Up: Vm.Nudge(0, -step); e.Handled = true; break;
            case Key.Down: Vm.Nudge(0, step); e.Handled = true; break;
        }
    }

    // --- Clipboard ---

    private async void OnCopyRequested(object? sender, string json)
    {
        var clipboard = Clipboard;
        if (clipboard is not null) await clipboard.SetTextAsync(json);
    }

    private async void OnPasteRequested(object? sender, EventArgs e)
    {
        var clipboard = Clipboard;
        if (clipboard is null) return;
        var text = await clipboard.GetTextAsync();
        if (text is not null) Vm?.Paste(text);
    }

    // --- Home screen file dialog ---

    private void OnHomeCommandRequestedFileDialog(object? sender, string commandId)
    {
        switch (commandId)
        {
            case ForgeCommands.OpenProject: _ = OpenProjectAsync(); break;
            case ForgeCommands.SaveProject: _ = SaveInternalAsync(); break;
        }
    }

    private void OnCustomizeHomeScreen(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _ = CustomizeHomeScreenAsync();

    private async Task CustomizeHomeScreenAsync()
    {
        if (Vm is null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a .nstudio file for the Home screen",
            AllowMultiple = false,
            FileTypeFilter = new[] { NstudioFileType }
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null) Vm.SetCustomHomeScreen(path);
    }

    // --- File operations ---

    private void OnNewProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.NewProject();

    private async void OnOpenProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        await OpenProjectAsync();

    private async Task OpenProjectAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Nyforge Project",
            AllowMultiple = false,
            FileTypeFilter = new[] { NstudioFileType }
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null) return;
        try { Vm?.OpenFromPath(path); }
        catch (NuiVersionMismatchException ex) { if (Vm is not null) Vm.StatusMessage = ex.Message; }
    }

    private async void OnSaveProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        await SaveInternalAsync();

    private async void OnSaveProjectAs(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        await SaveInternalAsync(forcePrompt: true);

    private async Task SaveInternalAsync(bool forcePrompt = false)
    {
        if (Vm is null) return;
        try { Vm.SaveToPath(); }
        catch (InvalidOperationException) { forcePrompt = true; }
        if (!forcePrompt) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Nyforge Project",
            DefaultExtension = "nstudio",
            SuggestedFileName = string.IsNullOrWhiteSpace(Vm.ProjectName) ? "Untitled" : Vm.ProjectName,
            FileTypeChoices = new[] { NstudioFileType }
        });
        var path = file?.TryGetLocalPath();
        if (path is not null) Vm.SaveToPath(path);
    }

    private void OnExportNui(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _ = SaveInternalAsync(forcePrompt: true);

    // --- Preview ---

    private void OnPreview(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var previewVm = Vm?.CreatePreview();
        if (previewVm is null) return;
        var window = new PreviewWindow { DataContext = previewVm };
        previewVm.CloseRequested += (_, _) => window.Close();
        window.Show(this);
    }

    private void OnRunInNyrqis(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null) return;
        var tmpPath = Path.Combine(Path.GetTempPath(), "nyrqis-runtime-preview.nstudio");
        Vm.SaveToPath(tmpPath);
        Vm.StatusMessage = $"Exported to {tmpPath} — load with: nyrqisctl nui load {tmpPath}";
    }

    // --- Theme toggle ---

    private void OnToggleTheme(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null) return;
        var next = Vm.CurrentTheme == "Eclipse" ? "Solar" : "Eclipse";
        Vm.SetThemeCommand.Execute(next);
    }

    // --- Validation ---

    private void OnValidate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null) return;
        var doc = Vm.GetDocumentForValidation();
        if (doc is null)
        {
            Vm.StatusMessage = "Cannot validate — no document loaded.";
            return;
        }
        var result = NuiValidator.Validate(doc);
        if (result.HasErrors)
        {
            var first = result.Errors.First();
            Vm.StatusMessage = $"Validation failed: {result.Errors.Count()} error(s) — {first.Message}";
        }
        else if (result.HasWarnings)
        {
            Vm.StatusMessage = $"Validation passed with {result.Warnings.Count()} warning(s).";
        }
        else
        {
            Vm.StatusMessage = "Validation passed — no issues found.";
        }
    }

    // --- Help ---

    private void OnHelpGettingStarted(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not null) Vm.StatusMessage = "See docs/getting-started.md in the Nyrqis repository.";
    }

    private void OnHelpSchemaReference(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is not null) Vm.StatusMessage = "See docs/reference/nui-schema/NUI-SCHEMA.md in the Nyrqis repository.";
    }

    // --- About ---

    private void OnAbout(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var about = new AboutWindow();
        about.ShowDialog(this);
    }

    // --- Settings / Preferences ---

    private void OnOpenPreferences(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.ShowDialog(this);
    }

    // --- Exit ---

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private static readonly FilePickerFileType NstudioFileType = new("Nyforge Project")
    {
        Patterns = new[] { "*.nstudio" }
    };
}
