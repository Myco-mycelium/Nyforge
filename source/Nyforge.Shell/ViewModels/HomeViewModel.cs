using System.Collections.ObjectModel;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Shell.Services;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// Renders an .nstudio document as part of Forge's own chrome, and routes
/// Button clicks to real Forge commands via id convention (ForgeCommands),
/// not the Behaviors/Events system — see ForgeCommands' doc comment for
/// why those two things are kept deliberately separate.
///
/// This is the concrete closing of NFM-000 §2.5 ("Re-skinning Forge is not
/// a special case"): the Home panel you're looking at right now is not
/// hardcoded Avalonia XAML — it's whatever .nstudio file
/// PreferencesService.HomeScreenPath points at, which you can open and
/// edit in Forge like any other project.
/// </summary>
public sealed class HomeViewModel : ViewModelBase
{
    public ObservableCollection<PreviewElementViewModel> Elements { get; } = new();

    private string _sourceDescription = string.Empty;
    public string SourceDescription
    {
        get => _sourceDescription;
        set => SetField(ref _sourceDescription, value);
    }

    public RelayCommand<string> InvokeCommandRequested { get; }

    public HomeViewModel(RelayCommand<string> invokeCommand)
    {
        InvokeCommandRequested = invokeCommand;
    }

    /// <summary>
    /// (Re)loads from the given path, or falls back to the bundled default
    /// example if path is null/missing. Errors are surfaced via
    /// SourceDescription rather than thrown — a broken custom Home screen
    /// should never prevent Forge itself from opening.
    /// </summary>
    public void LoadFrom(string? path, string bundledFallbackPath)
    {
        Elements.Clear();

        string? resolvedPath = null;
        NyforgeProject? project = null;
        string? problem = null;

        if (!string.IsNullOrEmpty(path))
        {
            if (File.Exists(path))
            {
                try
                {
                    project = NyforgeProject.Load(path);
                    resolvedPath = path;
                }
                catch (Exception ex)
                {
                    problem = $"Couldn't load custom Home screen ({ex.Message}).";
                }
            }
            else
            {
                problem = $"Custom Home screen not found: {path}.";
            }
        }

        if (project is null)
        {
            try
            {
                if (File.Exists(bundledFallbackPath))
                {
                    project = NyforgeProject.Load(bundledFallbackPath);
                    resolvedPath = bundledFallbackPath;
                }
            }
            catch
            {
                // Fall through to the empty-Home-screen state below rather
                // than throw — Forge itself must still open.
            }
        }

        if (project is null)
        {
            SourceDescription = problem is not null ? $"{problem} No Home screen available." : "No Home screen available.";
            return;
        }

        var root = project.Document.Screens.FirstOrDefault()?.Root;
        if (root is not null)
        {
            // v0.6: render the full tree, not just root children — Home
            // screens may nest, same as any other .nstudio document.
            void AddSubtree(NuiComponent node, double parentX, double parentY)
            {
                foreach (var child in node.Children)
                {
                    var absX = parentX + child.Layout.X;
                    var absY = parentY + child.Layout.Y;
                    Elements.Add(new PreviewElementViewModel(child, absX, absY));
                    AddSubtree(child, absX, absY);
                }
            }

            AddSubtree(root, 0, 0);
        }

        var renderedMessage = $"Rendering {Path.GetFileName(resolvedPath)} — File → Customize Home Screen... to change it.";
        SourceDescription = problem is not null ? $"{problem} Using the bundled default. {renderedMessage}" : renderedMessage;
    }

    /// <summary>Called by HomePanel's code-behind when a rendered Button's Id matches a ForgeCommands entry.</summary>
    public void InvokeIfCommand(string componentId)
    {
        if (ForgeCommands.All.Contains(componentId))
        {
            InvokeCommandRequested.Execute(componentId);
        }
        // Anything else is inert — an id that isn't a recognized command
        // just doesn't do anything, rather than guessing what was meant.
    }
}
