using System.Collections.ObjectModel;
using Nyforge.Core.Editing;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Shell.Services;

namespace Nyforge.Shell.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ProjectService _projectService;
    private readonly ThemeManager _themeManager;
    private readonly PreferencesService _preferences;

    public HomeViewModel Home { get; }

    /// <summary>
    /// Command-based undo/redo (v0.6, 2026-08-17 architecture review item
    /// #5): one IEditorCommand per completed gesture — a drag commits a
    /// single Move/Resize command on release, never a command per
    /// pointer-move. Every document edit flows through this; the Changed
    /// event refreshes the tree from the model.
    /// </summary>
    public CommandHistory History { get; } = new();

    public RelayCommand UndoCommand { get; }
    public RelayCommand RedoCommand { get; }
    public RelayCommand CopySelectionCommand { get; }
    public RelayCommand PasteCommand { get; }

    /// <summary>
    /// Raised when the Home screen's "Open Project" or "Save Project"
    /// buttons are clicked — those need a file dialog, which lives in
    /// MainWindow's code-behind (Views), not the ViewModel, per this
    /// project's existing split (see the OnOpenProject/OnSaveProject
    /// pattern already used by the File menu).
    /// </summary>
    public event EventHandler<string>? HomeCommandRequestedFileDialog;

    /// <summary>Copy produced a payload — the window writes it to the OS clipboard.</summary>
    public event EventHandler<string>? CopyRequested;

    /// <summary>Paste needs the OS clipboard — the window reads it, then calls <see cref="Paste"/>.</summary>
    public event EventHandler? PasteRequested;

    private int _pasteCascade;

    /// <summary>
    /// The component tree's top-level VMs (children of the screen root);
    /// nesting hangs off each VM's Children. Drives the Layers tree.
    /// </summary>
    public ObservableCollection<CanvasElementViewModel> CanvasElements { get; } = new();

    /// <summary>
    /// The same VMs flattened in depth-first order with absolute
    /// (canvas) positions — what the DesignCanvas draws. Kept in sync by
    /// RebuildRenderItems/RefreshRenderPositions; structural changes go
    /// through the former, pointer drags through the latter.
    /// </summary>
    public ObservableCollection<CanvasElementViewModel> CanvasRenderItems { get; } = new();

    /// <summary>All behaviors reachable from the current screen's component tree, v0.2 Logic Editor.</summary>
    public ObservableCollection<BehaviorViewModel> Behaviors { get; } = new();

    /// <summary>The v0.1 palette, sourced from the same contract table the schema doc references.</summary>
    public IReadOnlyList<ComponentContract> PaletteItems => ComponentContracts.All;

    private readonly ObservableCollection<CanvasElementViewModel> _selectedElements = new();

    /// <summary>All currently selected elements, in selection order (last = primary).</summary>
    public IReadOnlyList<CanvasElementViewModel> SelectedElements => _selectedElements;

    /// <summary>
    /// The primary selection — the most recently clicked element. Drives
    /// the Inspector and the single-selection Layers tree. The setter
    /// (bound from Layers, and used for programmatic single selects)
    /// replaces the whole selection; canvas multi-select goes through
    /// <see cref="SelectForInteraction"/>.
    /// </summary>
    public CanvasElementViewModel? SelectedElement
    {
        get => _selectedElements.Count > 0 ? _selectedElements[^1] : null;
        set
        {
            SetSelection(value is null ? Array.Empty<CanvasElementViewModel>() : new[] { value });
            OnSelectionChanged();
        }
    }

    public bool HasSelection => _selectedElements.Count > 0;
    public bool HasSingleSelection => _selectedElements.Count == 1;

    /// <summary>
    /// Canvas entry point for selection: additive (Ctrl/Cmd-click) toggles
    /// membership; non-additive replaces the selection with the element.
    /// </summary>
    public void SelectForInteraction(CanvasElementViewModel element, bool additive)
    {
        if (additive)
        {
            if (!_selectedElements.Remove(element)) _selectedElements.Add(element);
        }
        else
        {
            SetSelection(new[] { element });
        }
        OnSelectionChanged();
    }

    /// <summary>
    /// The selected elements with no selected ancestor — moving these by a
    /// delta moves the whole selection exactly once (children of a selected
    /// container ride along via the parent's own move).
    /// </summary>
    public IReadOnlyList<CanvasElementViewModel> TopmostSelected()
    {
        var selected = _selectedElements.ToList();
        return selected
            .Where(vm => !selected.Any(other => other != vm && IsDescendant(other, vm)))
            .ToList();
    }

    private void SetSelection(IEnumerable<CanvasElementViewModel> elements)
    {
        var newSet = elements.Distinct().ToList();
        foreach (var vm in _selectedElements) vm.IsSelected = false;
        _selectedElements.Clear();
        foreach (var vm in newSet)
        {
            _selectedElements.Add(vm);
            vm.IsSelected = true;
        }
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(SelectedElements));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasSingleSelection));
        OnPropertyChanged(nameof(AvailableEventsForSelection));
        OnPropertyChanged(nameof(NoUnboundEventsOnSelection));
        DeleteSelectedCommand.RaiseCanExecuteChanged();
        CopySelectionCommand.RaiseCanExecuteChanged();

        // The metadata-driven Inspector rows (one per property from the
        // Nyrqis API Registry); commits route through the history so
        // Inspector edits are undoable like canvas edits.
        if (SelectedElement is not null)
        {
            SelectedElement.RefreshPropertyEditors(CommitPropertyEdit);
        }
    }

    /// <summary>Wrap an Inspector property edit in an undoable
    /// ChangePropertyCommand (the metadata-driven Inspector's write
    /// path — the same history as drag/delete/reparent).</summary>
    private void CommitPropertyEdit(
        NuiComponent component, string property, object? oldValue, object? newValue)
    {
        History.Execute(new ChangePropertyCommand(component, property, oldValue, newValue));
    }

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

    /// <summary>Unbound events on the currently selected element — what AddBehaviorForSelectedCommand can attach to. Empty unless exactly one element is selected.</summary>
    public IReadOnlyList<string> AvailableEventsForSelection
    {
        get
        {
            if (!HasSingleSelection || SelectedElement is null) return Array.Empty<string>();
            if (!ComponentContracts.TryGet(SelectedElement.Type, out var contract)) return Array.Empty<string>();

            return contract!.Events
                .Where(evt => !SelectedElement.Model.Events.TryGetValue(evt, out var bound) || bound is null)
                .ToList();
        }
    }

    /// <summary>Every component id in the current screen's whole tree — valid Action targets besides "System".</summary>
    public IReadOnlyList<string> AvailableActionTargets
    {
        get
        {
            var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
            if (root is null) return new[] { "System" };
            return new[] { "System" }.Concat(ComponentTree.Walk(root).Select(n => n.Id)).ToList();
        }
    }

    /// <summary>True once a single element is selected but has nothing left to wire up — drives the BehaviorsPanel empty state.</summary>
    public bool NoUnboundEventsOnSelection => HasSingleSelection && AvailableEventsForSelection.Count == 0;

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
        UndoCommand = new RelayCommand(() => History.Undo(), () => History.CanUndo);
        RedoCommand = new RelayCommand(() => History.Redo(), () => History.CanRedo);
        CopySelectionCommand = new RelayCommand(CopySelection, () => HasSelection);
        PasteCommand = new RelayCommand(() => PasteRequested?.Invoke(this, EventArgs.Empty));

        // After any command (execute/undo/redo/clear) the model changed:
        // rebuild the VM tree + render list + behaviors from the model,
        // preserving selection by id, and re-evaluate undo/redo availability.
        History.Changed += (_, _) =>
        {
            RefreshTree();
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
        };

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
        var project = _projectService.Current;
        ProjectName = project.Document.Project.Name;

        // Undo never crosses a file boundary: loading (or new/open) resets
        // both stacks. Clear raises Changed, which refreshes the tree.
        History.Clear();

        if (TryResolveTheme(project.Document.Themes.Active, out var theme))
        {
            _themeManager.SetTheme(theme);
        }
    }

    /// <summary>
    /// Rebuilds the VM tree, flat render list, and Behaviors list from the
    /// current model — the single sync point for every model mutation that
    /// flows through <see cref="History"/>. Selection survives by id.
    /// </summary>
    private void RefreshTree()
    {
        var selectedIds = _selectedElements.Select(vm => vm.Id).ToHashSet();
        CanvasElements.Clear();
        Behaviors.Clear();

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is not null)
        {
            foreach (var child in root.Children)
            {
                CanvasElements.Add(BuildTree(child, null));
            }
            RebuildRenderItems();
            RebuildBehaviors(root);
        }
        else
        {
            RebuildRenderItems();
        }

        // Selection survives by id — including multi-selections.
        var matches = CanvasRenderItems.Where(vm => selectedIds.Contains(vm.Id)).ToList();
        SetSelection(matches);
        OnSelectionChanged();
        OnPropertyChanged(nameof(AvailableActionTargets));
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

    /// <summary>
    /// Builds the VM tree for one model node (and its descendants),
    /// mirroring the model hierarchy so the canvas, Layers, and reparent
    /// operations all share the same structure.
    /// </summary>
    private static CanvasElementViewModel BuildTree(NuiComponent model, CanvasElementViewModel? parent)
    {
        var vm = new CanvasElementViewModel(model, parent);
        foreach (var child in model.Children)
        {
            vm.Children.Add(BuildTree(child, vm));
        }
        return vm;
    }

    /// <summary>
    /// Rebuilds the flat render list from the VM tree after a structural
    /// change (load/add/delete/reparent) and recomputes absolute positions.
    /// </summary>
    private void RebuildRenderItems()
    {
        CanvasRenderItems.Clear();
        void Add(CanvasElementViewModel vm)
        {
            vm.RenderX = (vm.Parent?.RenderX ?? 0) + vm.X;
            vm.RenderY = (vm.Parent?.RenderY ?? 0) + vm.Y;
            CanvasRenderItems.Add(vm);
            foreach (var child in vm.Children) Add(child);
        }
        foreach (var rootVm in CanvasElements) Add(rootVm);
    }

    /// <summary>
    /// Recomputes absolute positions without touching the collection
    /// (pointer drags call this every move; the list is already correct).
    /// </summary>
    public void RefreshRenderPositions()
    {
        void Walk(CanvasElementViewModel vm)
        {
            vm.RenderX = (vm.Parent?.RenderX ?? 0) + vm.X;
            vm.RenderY = (vm.Parent?.RenderY ?? 0) + vm.Y;
            foreach (var child in vm.Children) Walk(child);
        }
        foreach (var rootVm in CanvasElements) Walk(rootVm);
    }

    /// <summary>
    /// The deepest container under the given canvas point that isn't the
    /// dragged element or inside its subtree — the drop target for
    /// reparenting. Null means "the screen root" (drop on empty canvas).
    /// </summary>
    public CanvasElementViewModel? ContainerAt(double x, double y, CanvasElementViewModel? exclude)
    {
        CanvasElementViewModel? best = null;
        void Consider(CanvasElementViewModel vm)
        {
            if (!vm.CanContainChildren) return;
            // Skip the dragged element itself and anything inside its own
            // subtree (that would create a cycle). Its ancestors stay
            // eligible — dropping back onto your own parent is a no-op.
            if (vm == exclude || IsDescendant(exclude, vm)) return;
            if (x >= vm.RenderX && x <= vm.RenderX + vm.Width &&
                y >= vm.RenderY && y <= vm.RenderY + vm.Height)
            {
                if (best is null || vm.Depth > best.Depth) best = vm;
            }
        }
        void Walk(CanvasElementViewModel vm)
        {
            Consider(vm);
            foreach (var child in vm.Children) Walk(child);
        }
        foreach (var rootVm in CanvasElements) Walk(rootVm);
        return best;
    }

    private static bool IsDescendant(CanvasElementViewModel? ancestor, CanvasElementViewModel? node)
    {
        if (ancestor is null || node is null) return false;
        foreach (var child in ancestor.Children)
        {
            if (child == node || IsDescendant(child, node)) return true;
        }
        return false;
    }

    /// <summary>
    /// The topmost non-container sibling of the dragged element under the
    /// given canvas point — the drop target for reordering (z-order).
    /// Containers are reparent targets instead; the element itself and its
    /// own subtree are excluded.
    /// </summary>
    public CanvasElementViewModel? SiblingAt(double x, double y, CanvasElementViewModel? exclude)
    {
        for (var i = CanvasRenderItems.Count - 1; i >= 0; i--) // topmost first
        {
            var vm = CanvasRenderItems[i];
            if (vm == exclude) continue;
            if (vm.CanContainChildren) continue; // containers are reparent targets
            if (vm.Parent != exclude?.Parent) continue; // must share the same parent
            if (exclude is not null && IsDescendant(exclude, vm)) continue;
            if (x >= vm.RenderX && x <= vm.RenderX + vm.Width &&
                y >= vm.RenderY && y <= vm.RenderY + vm.Height)
            {
                return vm;
            }
        }
        return null;
    }

    /// <summary>
    /// v0.6 drag-to-reorder: moves an element immediately before a sibling
    /// in the shared parent's Children list (z-order), as one undoable
    /// command. Returns false for non-siblings or no-ops.
    /// </summary>
    public bool TryReorder(CanvasElementViewModel element, CanvasElementViewModel target)
    {
        if (element == target || element.Parent != target.Parent) return false;

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return false;

        var (parent, oldIndex) = ComponentTree.FindParentAndIndex(root, element.Model.Id);
        if (parent is null) return false;
        var targetIndex = parent.Children.IndexOf(target.Model);
        if (targetIndex < 0 || targetIndex == oldIndex) return false;

        History.Execute(new ReorderComponentCommand(parent, element.Model, oldIndex, targetIndex));
        _projectService.Current.MarkDirty();
        StatusMessage = $"Reordered {element.Id} before {target.Id}.";
        return true;
    }

    /// <summary>
    /// Arrow-key nudging: moves the whole selection by (dx, dy) as one
    /// undoable command per key press (4 px step, Shift for larger).
    /// </summary>
    public void Nudge(double dx, double dy)
    {
        if (!HasSelection) return;

        var commands = TopmostSelected()
            .Select(vm => (IEditorCommand)new MoveComponentCommand(
                vm.Model, vm.X, vm.Y, Math.Max(0, vm.X + dx), Math.Max(0, vm.Y + dy)))
            .ToList();
        if (commands.Count == 0) return;
        if (commands.Count == 1) History.Execute(commands[0]);
        else History.Execute(new CompositeCommand(commands));
        _projectService.Current.MarkDirty();
    }

    /// <summary>
    /// v0.6: move a component to a new parent (a container VM, or null for
    /// the screen root), preserving its absolute canvas position. Pure no-op
    /// when the target is the current parent or the move is impossible.
    /// </summary>
    public bool TryReparent(CanvasElementViewModel element, CanvasElementViewModel? newParent)
    {
        if (element.Parent == newParent) return false;

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return false;

        // Capture the pre-move position before the command mutates anything.
        var (oldParent, oldIndex) = ComponentTree.FindParentAndIndex(root, element.Model.Id);
        if (oldParent is null) return false;

        var target = newParent?.Model ?? root;
        if (!ComponentTree.CanContainChildren(target.Type)) return false;
        if (ComponentTree.Find(element.Model, target.Id) is not null) return false; // cycle

        // One command per gesture: the move + position-preserving reparent,
        // undoable back to the exact old parent and z-order.
        History.Execute(new ReparentComponentCommand(root, element.Model, oldParent, oldIndex, target));
        _projectService.Current.MarkDirty();
        StatusMessage = newParent is null
            ? $"Moved {element.Id} to the root."
            : $"Moved {element.Id} into {newParent.Id}.";
        return true;
    }

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

        // v0.6: adding into the selected container when one is selected,
        // otherwise into the screen root — the first step of nested editing.
        var container = SelectedElement is { CanContainChildren: true } sel ? sel : null;
        var parentModel = container?.Model ?? root;

        var component = new NuiComponent
        {
            Id = $"{componentType.ToLowerInvariant()}_{Guid.NewGuid().ToString("N")[..6]}",
            Type = componentType,
            Layout = new NuiLayout
            {
                X = container is null ? 40 : 16,
                Y = container is null ? 40 : 16,
                Width = DefaultWidthFor(componentType),
                Height = DefaultHeightFor(componentType)
            }
        };
        if (componentType is "Text" or "Button" or "Link" or "Checkbox" or "Radio" or "Toggle")
        {
            component.Properties["text"] = componentType;
        }

        History.Execute(new AddComponentCommand(parentModel, component));
        SelectedElement = CanvasRenderItems.FirstOrDefault(vm => vm.Model == component);
        _projectService.Current.MarkDirty();
        StatusMessage = container is null ? $"Added {componentType}." : $"Added {componentType} into {container.Id}.";
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

    /// <summary>
    /// v0.6 copy/paste: copies the topmost-selected subtrees (children of
    /// a selected container ride along) as a JSON payload the window writes
    /// to the OS clipboard. Behaviors never travel: they are document-scoped.
    /// </summary>
    public void CopySelection()
    {
        if (!HasSelection) return;
        var topmost = TopmostSelected();
        var payload = ComponentClipboard.Serialize(topmost.Select(vm => vm.Model));
        _pasteCascade = 0;
        CopyRequested?.Invoke(this, payload);
        StatusMessage = $"Copied {topmost.Count} element{(topmost.Count == 1 ? "" : "s")}.";
    }

    /// <summary>
    /// Pastes a clipboard payload into the selected container (or the root),
    /// one undoable command per paste. Pasted components get fresh ids and
    /// arrive unbound; each paste cascades 8 px so repeats stay visible.
    /// </summary>
    public void Paste(string? json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var parsed = ComponentClipboard.Deserialize(json);
        if (parsed.Count == 0)
        {
            StatusMessage = "Clipboard doesn't contain NUI components.";
            return;
        }

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return;

        var container = SelectedElement is { CanContainChildren: true } sel ? sel : null;
        var parentModel = container?.Model ?? root;

        var clones = ComponentClipboard.CloneWithFreshIds(parsed);
        var offset = ++_pasteCascade * 8.0;
        foreach (var clone in clones)
        {
            clone.Layout.X += offset;
            clone.Layout.Y += offset;
        }

        var commands = clones
            .Select(c => (IEditorCommand)new AddComponentCommand(parentModel, c))
            .ToList();
        if (commands.Count == 1) History.Execute(commands[0]);
        else History.Execute(new CompositeCommand(commands));

        // Select what was pasted.
        var pasted = CanvasRenderItems.Where(vm => clones.Contains(vm.Model)).ToList();
        for (var i = 0; i < pasted.Count; i++)
        {
            SelectForInteraction(pasted[i], additive: i > 0);
        }

        _projectService.Current.MarkDirty();
        StatusMessage = container is null
            ? $"Pasted {clones.Count} element{(clones.Count == 1 ? "" : "s")}."
            : $"Pasted {clones.Count} element{(clones.Count == 1 ? "" : "s")} into {container.Id}.";
    }

    public void DeleteSelected()
    {
        if (!HasSelection) return;

        var root = _projectService.Current.Document.Screens.FirstOrDefault()?.Root;
        if (root is null) return;

        // One composite command for the whole multi-delete: each element's
        // parent/index is captured before anything moves. Nested selections
        // (a container + a child) delete cleanly because the child's
        // command removes from the detached container's own Children list.
        var commands = new List<IEditorCommand>();
        foreach (var vm in _selectedElements.ToList())
        {
            var (parent, index) = ComponentTree.FindParentAndIndex(root, vm.Model.Id);
            if (parent is not null)
            {
                commands.Add(new DeleteComponentCommand(
                    _projectService.Current.Document, parent, index, vm.Model));
            }
        }

        if (commands.Count == 1)
        {
            History.Execute(commands[0]);
        }
        else if (commands.Count > 1)
        {
            History.Execute(new CompositeCommand(commands));
        }
        _projectService.Current.MarkDirty();
        StatusMessage = commands.Count > 1 ? $"Deleted {commands.Count} elements." : "Deleted element.";
    }

    /// <summary>
    /// v0.2 Logic Editor entry point: attach a new, default-empty behavior
    /// to the given unbound event on the currently selected element.
    /// </summary>
    public void AddBehaviorForSelected(string? eventName)
    {
        if (string.IsNullOrEmpty(eventName) || !HasSingleSelection || SelectedElement is null) return;

        var behavior = new NuiBehavior
        {
            Id = $"behavior_{Guid.NewGuid().ToString("N")[..8]}",
            Action = new NuiAction { Target = "System", Name = NuiSystemActions.All.First().Name }
        };

        History.Execute(new AddBehaviorCommand(
            _projectService.Current.Document, SelectedElement.Model, eventName, behavior));

        _projectService.Current.MarkDirty();
        StatusMessage = $"Added behavior for {SelectedElement.Id}.{eventName}.";
    }

    public void RemoveBehavior(BehaviorViewModel? behavior)
    {
        if (behavior is null) return;

        History.Execute(new DeleteBehaviorCommand(
            _projectService.Current.Document, behavior.SourceComponent, behavior.EventName, behavior.Model));

        _projectService.Current.MarkDirty();
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
