using System.Collections.ObjectModel;
using Nyforge.Core.Nui;
using Nyforge.Shell.Services;

namespace Nyforge.Shell.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ProjectService _projectService;
    private readonly ThemeManager _themeManager;
    private readonly PreferencesService _preferences;

    public HomeViewModel Home { get; }

    /// <summary>
    /// Raised when the Home screen's "Open Project" or "Save Project"
    /// buttons are clicked — those need a file dialog, which lives in
    /// MainWindow's code-behind (Views), not the ViewModel, per this
    /// project's existing split (see the OnOpenProject/OnSaveProject
    /// pattern already used by the File menu).
    /// </summary>
    public event EventHandler<string>? HomeCommandRequestedFileDialog;

    public ObservableCollection<CanvasElementViewModel> CanvasElements { get; } = new();

    /// <summary>All behaviors reachable from the current screen's component tree, v0.2 Logic Editor.</summary>
    public ObservableCollection<BehaviorViewModel> Behaviors { get; } = new();

    /// <summary>The v0.1 palette, sourced from the same contract table the schema doc references.</summary>
    public IReadOnlyList<ComponentContract> PaletteItems => ComponentContracts.All;

    private CanvasElementViewModel? _selectedElement;
    public CanvasElementViewModel? SelectedElement
    {
        get => _selectedElement;
        set
        {
            if (_selectedElement is not null) _selectedElement.IsSelected = false;
            SetField(ref _selectedElement, value);
            if (_selectedElement is not null) _selectedElement.IsSelected = true;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(AvailableEventsForSelection));
            OnPropertyChanged(nameof(NoUnboundEventsOnSelection));
        }
    }

    public bool HasSelection => SelectedElement is not null;

    private string _projectName = "Untitled Project";
    public string ProjectName
    {
        get => _projectName;
        set => SetField(ref _projectName, value);
    }

    private string _statusMessage = "Ready.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string CurrentTheme => _themeManager.CurrentTheme;

    public RelayCommand NewProjectCommand { get; }
    public RelayCommand DeleteSelectedCommand { get; }
    public RelayCommand<string> AddComponentCommand { get; }
    public RelayCommand<string> SetThemeCommand { get; }
    public RelayCommand<string> AddBehaviorForSelectedCommand { get; }
    public RelayCommand<BehaviorViewModel> RemoveBehaviorCommand { get; }

    /// <summary>Unbound events on the currently selected element — what AddBehaviorForSelectedCommand can attach to.</summary>
    public IReadOnlyList<string> AvailableEventsForSelection
    {
        get
        {
            if (SelectedElement is null) return Array.Empty<string>();
            if (!ComponentContracts.TryGet(SelectedElement.Type, out var contract)) return Array.Empty<string>();

            return contract!.Events
                .Where(evt => !SelectedElement.Model.Events.TryGetValue(evt, out var bound) || bound is null)
                .ToList();
        }
    }

    /// <summary>Every component id on the current screen — valid Action targets besides "System".</summary>
    public IReadOnlyList<string> AvailableActionTargets =>
        new[] { "System" }.Concat(CanvasElements.Select(e => e.Id)).ToList();

    /// <summary>True once an element is selected but has nothing left to wire up — drives the BehaviorsPanel empty state.</summary>
    public bool NoUnboundEventsOnSelection => HasSelection && AvailableEventsForSelection.Count == 0;

    public MainWindowViewModel(ProjectService projectService, ThemeManager themeManager, PreferencesService preferences)
    {
        _projectService = projectService;
        _themeManager = themeManager;
        _preferences = preferences;
        _themeManager.ThemeChanged += (_, _) => OnPropertyChanged(nameof(CurrentTheme));

        NewProjectCommand = new RelayCommand(NewProject);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => HasSelection);
        AddComponentCommand = new RelayCommand<string>(AddComponent);
        SetThemeCommand = new RelayCommand<string>(name => { if (name is not null) _themeManager.SetTheme(name); });
        AddBehaviorForSelectedCommand = new RelayCommand<string>(AddBehaviorForSelected);
        RemoveBehaviorCommand = new RelayCommand<BehaviorViewModel>(RemoveBehavior);

        Home = new HomeViewModel(new RelayCommand<string>(OnHomeCommand));
        ReloadHomeScreen();

        LoadFromProject();
    }

    private void OnHomeCommand(string? commandId)
    {
        switch (commandId)
        {
            case ForgeCommands.NewProject:
                NewProject();
                break;
            case ForgeCommands.OpenProject:
            case ForgeCommands.SaveProject:
                // These need a file dialog, which lives in MainWindow's
                // code-behind — see HomeCommandRequestedFileDialog's doc comment.
                HomeCommandRequestedFileDialog?.Invoke(this, commandId);
                break;
        }
    }

    /// <summary>
    /// Reloads Home from PreferencesService.HomeScreenPath (or the bundled
    /// default if unset/missing). Call after "Customize Home Screen..." so
    /// the change is visible immediately, not just on next restart.
    /// </summary>
    public void ReloadHomeScreen()
    {
        var bundledDefault = Path.Combine(AppContext.BaseDirectory, "examples", "forge-home", "forge-home.nstudio");
        Home.LoadFrom(_preferences.HomeScreenPath, bundledDefault);
    }

    public void SetCustomHomeScreen(string path)
    {
        _preferences.HomeScreenPath = path;
        ReloadHomeScreen();
    }

    private void LoadFromProject()
    {
        CanvasElements.Clear();
        SelectedElement = null;
        Behaviors.Clear();

        var project = _projectService.Current;
        ProjectName = project.Document.Project.Name;

        var root = project.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return;

        foreach (var child in root.Children)
        {
            CanvasElements.Add(new CanvasElementViewModel(child));
        }

        RebuildBehaviors(root);

        if (TryResolveTheme(project.Document.Themes.Active, out var theme))
        {
            _themeManager.SetTheme(theme);
        }
    }

    /// <summary>
    /// Walks the component tree looking for events bound to a behavior id,
    /// resolves each against Document.Behaviors, and rebuilds the flat
    /// Behaviors list the Logic Editor displays. v0.1's canvas only shows
    /// root-level children (see CanvasElementViewModel's scope note), but
    /// this walk covers the full tree so hand-authored nested behaviors
    /// (like examples/settings-app) still show up correctly.
    /// </summary>
    private void RebuildBehaviors(NuiComponent node)
    {
        var byId = _projectService.Current.Document.Behaviors.ToDictionary(b => b.Id, b => b);

        void Walk(NuiComponent component)
        {
            foreach (var (eventName, behaviorId) in component.Events)
            {
                if (behaviorId is null) continue;
                if (byId.TryGetValue(behaviorId, out var behavior))
                {
                    Behaviors.Add(new BehaviorViewModel(behavior, component, eventName, ResolveComponentType));
                }
            }

            foreach (var child in component.Children)
            {
                Walk(child);
            }
        }

        Walk(node);
    }

    private string? ResolveComponentType(string componentId)
    {
        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return null;

        string? Find(NuiComponent node)
        {
            if (node.Id == componentId) return node.Type;
            foreach (var child in node.Children)
            {
                var found = Find(child);
                if (found is not null) return found;
            }
            return null;
        }

        return Find(root);
    }

    private static bool TryResolveTheme(string? name, out string theme)
    {
        theme = name ?? "Eclipse";
        return ThemeManager.AvailableThemes.ContainsKey(theme);
    }

    public void NewProject()
    {
        _projectService.NewProject();
        LoadFromProject();
        StatusMessage = "New project created.";
    }

    public void OpenFromPath(string path)
    {
        _projectService.Open(path);
        LoadFromProject();
        StatusMessage = $"Opened {Path.GetFileName(path)}.";
    }

    public void SaveToPath(string? path = null)
    {
        SyncSelectedProjectRoot();
        _projectService.Current.Document.Themes.Active = _themeManager.CurrentTheme;
        _projectService.Save(path);
        StatusMessage = $"Saved {Path.GetFileName(_projectService.Current.FilePath)}.";
    }

    /// <summary>
    /// v0.1's canvas edits NuiComponent instances by reference (see
    /// CanvasElementViewModel), so the document tree is already current —
    /// this exists as an explicit seam for v0.2, when the canvas may hold
    /// a design-time-only copy that needs to be written back before save.
    /// </summary>
    private void SyncSelectedProjectRoot() { }

    public void AddComponent(string? componentType)
    {
        if (string.IsNullOrEmpty(componentType)) return;
        if (!ComponentContracts.TryGet(componentType, out _))
        {
            StatusMessage = $"Unknown component type '{componentType}'.";
            return;
        }

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return;

        var component = new NuiComponent
        {
            Id = $"{componentType.ToLowerInvariant()}_{Guid.NewGuid().ToString("N")[..6]}",
            Type = componentType,
            Layout = new NuiLayout { X = 40, Y = 40, Width = DefaultWidthFor(componentType), Height = DefaultHeightFor(componentType) }
        };
        if (componentType is "Text" or "Button" or "Link" or "Checkbox" or "Radio" or "Toggle")
        {
            component.Properties["text"] = componentType;
        }

        root.Children.Add(component);
        var vm = new CanvasElementViewModel(component);
        CanvasElements.Add(vm);
        SelectedElement = vm;
        _projectService.Current.MarkDirty();
        StatusMessage = $"Added {componentType}.";
        OnPropertyChanged(nameof(AvailableActionTargets));
    }

    private static double DefaultWidthFor(string type) => type switch
    {
        "Sidebar" or "NavigationRail" => 220,
        "Container" or "Card" or "Panel" or "Toolbar" or "StatusBar" or "SplitView" or "ScrollView" or "Grid" or "Stack" or "FlexLayout" => 320,
        "Window" or "Dialog" => 480,
        _ => 140
    };

    private static double DefaultHeightFor(string type) => type switch
    {
        "Sidebar" or "NavigationRail" => 480,
        "Container" or "Card" or "Panel" or "SplitView" or "ScrollView" or "Grid" or "Stack" or "FlexLayout" => 220,
        "Window" or "Dialog" => 320,
        "Slider" or "ProgressBar" => 24,
        _ => 32
    };

    public void DeleteSelected()
    {
        if (SelectedElement is null) return;

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        root?.Children.Remove(SelectedElement.Model);
        CanvasElements.Remove(SelectedElement);

        // Clean up any behaviors reachable only from the deleted subtree
        // (the element itself, plus any nested children it carried) — both
        // the Logic Editor's display list AND the underlying saved
        // document, so a saved .nstudio file never keeps a dangling
        // Behaviors entry for an event on a component that no longer exists.
        var deletedSubtree = CollectSubtree(SelectedElement.Model).ToHashSet();
        var orphaned = Behaviors.Where(b => deletedSubtree.Contains(b.SourceComponent)).ToList();
        foreach (var b in orphaned)
        {
            Behaviors.Remove(b);
            _projectService.Current.Document.Behaviors.Remove(b.Model);
        }

        SelectedElement = null;
        _projectService.Current.MarkDirty();
        StatusMessage = "Deleted element.";
        OnPropertyChanged(nameof(AvailableActionTargets));
    }

    private static IEnumerable<NuiComponent> CollectSubtree(NuiComponent node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            foreach (var descendant in CollectSubtree(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// v0.2 Logic Editor entry point: attach a new, default-empty behavior
    /// to the given unbound event on the currently selected element.
    /// </summary>
    public void AddBehaviorForSelected(string? eventName)
    {
        if (string.IsNullOrEmpty(eventName) || SelectedElement is null) return;

        var behavior = new NuiBehavior
        {
            Id = $"behavior_{Guid.NewGuid().ToString("N")[..8]}",
            Action = new NuiAction { Target = "System", Name = NuiSystemActions.All.First().Name }
        };

        _projectService.Current.Document.Behaviors.Add(behavior);
        SelectedElement.Model.Events[eventName] = behavior.Id;

        var vm = new BehaviorViewModel(behavior, SelectedElement.Model, eventName, ResolveComponentType);
        Behaviors.Add(vm);

        _projectService.Current.MarkDirty();
        OnPropertyChanged(nameof(AvailableEventsForSelection));
        OnPropertyChanged(nameof(NoUnboundEventsOnSelection));
        StatusMessage = $"Added behavior for {SelectedElement.Id}.{eventName}.";
    }

    public void RemoveBehavior(BehaviorViewModel? behavior)
    {
        if (behavior is null) return;

        behavior.SourceComponent.Events[behavior.EventName] = null;
        _projectService.Current.Document.Behaviors.Remove(behavior.Model);
        Behaviors.Remove(behavior);

        _projectService.Current.MarkDirty();
        OnPropertyChanged(nameof(AvailableEventsForSelection));
        OnPropertyChanged(nameof(NoUnboundEventsOnSelection));
        StatusMessage = "Removed behavior.";
    }

    /// <summary>
    /// v0.3: builds a fresh PreviewViewModel from the current in-memory
    /// document. Preview reads live editor state (including the active
    /// theme) but never writes back to it — see PreviewViewModel's own
    /// doc comment.
    /// </summary>
    public PreviewViewModel CreatePreview()
    {
        _projectService.Current.Document.Themes.Active = _themeManager.CurrentTheme;
        return new PreviewViewModel(_projectService.Current, _themeManager);
    }
}
