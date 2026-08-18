using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
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

        // MainWindow is constructed before its DataContext is assigned
        // (App.axaml.cs sets it via object initializer right after `new
        // MainWindow`), so subscribing here — rather than assuming the
        // DataContext is already set — reliably catches that assignment
        // via the DataContextChanged event once it happens.
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
        // Don't hijack arrows while the user is typing (Inspector, etc.).
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

    // --- Clipboard (OS clipboard IO lives here, not the ViewModel) ---

    private async void OnCopyRequested(object? sender, string json)
    {
        var clipboard = Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(json);
        }
    }

    private async void OnPasteRequested(object? sender, EventArgs e)
    {
        var clipboard = Clipboard;
        if (clipboard is null) return;
        var text = await clipboard.GetTextAsync();
        if (text is not null)
        {
            Vm?.Paste(text);
        }
    }

    private void OnHomeCommandRequestedFileDialog(object? sender, string commandId)
    {
        // Route Home screen's "Open Project"/"Save Project" buttons through
        // the exact same file-dialog flows the File menu uses — see
        // ForgeCommands' doc comment for why this is id-based rather than
        // going through the Behaviors/Events system.
        switch (commandId)
        {
            case ForgeCommands.OpenProject:
                _ = OpenProjectAsync();
                break;
            case ForgeCommands.SaveProject:
                _ = SaveInternalAsync();
                break;
        }
    }

    private void OnCustomizeHomeScreen(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _ = CustomizeHomeScreenAsync();

    private async Task CustomizeHomeScreenAsync()
    {
        if (Vm is null) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a .nstudio file to render as Forge's Home screen",
            AllowMultiple = false,
            FileTypeFilter = new[] { NstudioFileType }
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null) return;

        Vm.SetCustomHomeScreen(path);
    }

    private void OnNewProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        Vm?.NewProject();

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

        try
        {
            Vm?.OpenFromPath(path);
        }
        catch (NuiVersionMismatchException ex)
        {
            if (Vm is not null) Vm.StatusMessage = ex.Message;
        }
    }

    private async void OnSaveProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Vm is null) return;
        // SaveInternalAsync tries a direct save first and only prompts for
        // a path (Save As behavior) if the project has never been saved —
        // see the InvalidOperationException catch below.
        await SaveInternalAsync();
    }

    private async void OnSaveProjectAs(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SaveInternalAsync(forcePrompt: true);
    }

    private async Task SaveInternalAsync(bool forcePrompt = false)
    {
        if (Vm is null) return;

        try
        {
            Vm.SaveToPath();
        }
        catch (InvalidOperationException)
        {
            forcePrompt = true;
        }

        if (!forcePrompt) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Nyforge Project",
            DefaultExtension = "nstudio",
            SuggestedFileName = string.IsNullOrWhiteSpace(Vm.ProjectName) ? "Untitled" : Vm.ProjectName,
            FileTypeChoices = new[] { NstudioFileType }
        });

        var path = file?.TryGetLocalPath();
        if (path is null) return;

        Vm.SaveToPath(path);
    }

    private void OnExportNui(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // v0.1: exporting the NUI document IS saving the .nstudio file — see
        // docs/reference/nui-schema/NUI-SCHEMA.md. Separate export targets
        // (native code generators) are v0.2+.
        _ = SaveInternalAsync(forcePrompt: true);
    }

    private void OnPreview(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var previewVm = Vm?.CreatePreview();
        if (previewVm is null) return;

        var window = new PreviewWindow { DataContext = previewVm };
        previewVm.CloseRequested += (_, _) =>
        {
            // v0.3 preview only ever has one window (the screen root), so
            // any Close action just closes the preview — honest about not
            // supporting multi-window semantics yet, rather than pretending to.
            window.Close();
        };
        window.Show(this);
    }

    private void OnRunInNyrqis(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Export the current .nstudio document to the Nyrqis runtime
        // (ui/runtime.py). For now, save the document to a temp file
        // and show the path — the actual IPC integration with the
        // Nyrqis daemon is a follow-on (doc #29).
        if (Vm is null) return;

        var tmpPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "nyrqis-runtime-preview.nstudio");
        Vm.SaveToPath(tmpPath);
        Vm.StatusMessage = $"Exported to {tmpPath} — load with: nyrqisctl nui load {tmpPath}";
    }

    private static readonly FilePickerFileType NstudioFileType = new("Nyforge Project")
    {
        Patterns = new[] { "*.nstudio" }
    };
}
