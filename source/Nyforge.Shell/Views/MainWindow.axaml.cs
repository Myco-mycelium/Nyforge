using Avalonia.Controls;
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
            }
        };
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

    private static readonly FilePickerFileType NstudioFileType = new("Nyforge Project")
    {
        Patterns = new[] { "*.nstudio" }
    };
}
