using System.Collections.ObjectModel;
using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;
using Nyforge.Shell.Renderers;
using Nyforge.Shell.Services;

namespace Nyforge.Shell.ViewModels;

/// <summary>
/// A design-time-only wrapper around a NuiComponent for the Preview
/// window: unlike CanvasElementViewModel (which edits the saved document),
/// this holds a live, ephemeral copy of interactive property state so
/// clicking/toggling things in Preview doesn't mutate the project you're
/// editing. Preview state resets every time you reopen it.
/// </summary>
public sealed class PreviewElementViewModel : ViewModelBase
{
    public NuiComponent Model { get; }

    public string Id => Model.Id;
    public string Type => Model.Type;

    /// <summary>
    /// Absolute canvas position (parent-relative Layout offsets summed up
    /// the chain, per NUI-SCHEMA §3) — v0.6 renders nested trees.
    /// </summary>
    public double X { get; }
    public double Y { get; }
    public double Width => Model.Layout.Width;
    public double Height => Model.Layout.Height;

    private bool _boolValue;
    public bool BoolValue
    {
        get => _boolValue;
        set => SetField(ref _boolValue, value);
    }

    private string _text;
    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public PreviewElementViewModel(NuiComponent model, double x, double y)
    {
        Model = model;
        X = x;
        Y = y;
        _text = model.Properties.TryGetValue("text", out var t) ? t?.ToString() ?? model.Type : model.Type;
        _boolValue = model.Properties.TryGetValue("value", out var v) && v is bool b && b;
    }
}

/// <summary>
/// Runtime state and event dispatch for the Preview window. This is
/// Forge's honest stand-in for "running" the app — see NFM-000 §2.1 and
/// MainWindow.axaml.cs's OnPreview. It reads the saved NuiDocument but
/// never writes back to it; closing Preview discards all runtime state.
///
/// All dispatch goes through <see cref="ForgePreviewRuntime"/> (the
/// <see cref="INuiRuntime"/> seam), so the same application logic runs
/// identically here and in a future Nyrqis runtime (doc #8/#9).
/// </summary>
public sealed class PreviewViewModel : ViewModelBase
{
    private readonly NyforgeProject _project;
    private readonly ForgePreviewRuntime _runtime;
    private readonly Dictionary<string, PreviewElementViewModel> _byId = new();

    public ObservableCollection<PreviewElementViewModel> Elements { get; } = new();
    public ObservableCollection<string> Log { get; } = new();

    /// <summary>
    /// The renderer registry driving Preview's component rendering.
    /// </summary>
    public ComponentRendererRegistry Renderers { get; } = null!;

    public event EventHandler<string>? CloseRequested;

    public PreviewViewModel(NyforgeProject project, ThemeManager themeManager)
    {
        _project = project;

        // ForgePreviewRuntime owns the runtime state and dispatches
        // through INuiRuntime — the same seam the real Nyrqis runtime
        // will implement (doc #8/#9).
        _runtime = new ForgePreviewRuntime(project, themeManager);

        // Register Forge's Avalonia renderers so the Preview's type
        // converters are driven by the registry, not hardcoded sets.
        Renderers = ForgeRendererRegistry.Create();
        PreviewTypeConverters.Initialise(Renderers);

        // Wire runtime log and close events to our public collections.
        _runtime.PropertyChanged += OnRuntimePropertyChanged;
        _runtime.CloseRequested += windowId => CloseRequested?.Invoke(this, windowId);

        var root = project.Document.Screens.FirstOrDefault()?.Root;
        if (root is not null)
        {
            AddSubtree(root, 0, 0);
        }

        SeedBindings();
        Log.Insert(0, "Preview started (Forge's own renderer — not the real Nyrqis UI Runtime).");
    }

    /// <summary>
    /// v0.6: flattens the whole component tree (not just the screen
    /// root's children) into absolute-positioned preview elements — the
    /// same math the canvas uses, so what you design is what previews.
    /// </summary>
    private void AddSubtree(NuiComponent node, double parentX, double parentY)
    {
        foreach (var child in node.Children)
        {
            var absX = parentX + child.Layout.X;
            var absY = parentY + child.Layout.Y;
            var vm = new PreviewElementViewModel(child, absX, absY);
            Elements.Add(vm);
            _byId[child.Id] = vm;
            AddSubtree(child, absX, absY);
        }
    }

    /// <summary>Seed each bound property from its current state value, per NUI-SCHEMA.md §8.</summary>
    private void SeedBindings()
    {
        foreach (var binding in _project.Document.Bindings)
        {
            _runtime.ApplyBinding(binding);
        }
    }

    private void OnRuntimePropertyChanged(string componentId, string property, object? value)
    {
        if (!_byId.TryGetValue(componentId, out var element)) return;
        ApplyValueToProperty(element, property, value);
    }

    private static void ApplyValueToProperty(PreviewElementViewModel element, string property, object? value)
    {
        switch (property)
        {
            case "value" when value is bool b:
                element.BoolValue = b;
                break;
            case "value" or "text":
                element.Text = value?.ToString() ?? string.Empty;
                break;
        }
    }

    /// <summary>
    /// Call this whenever a Preview control's interactive value changes
    /// (Toggle flipped, Checkbox clicked, etc.) — updates any bound state,
    /// then fires the component's event.
    /// </summary>
    public void OnPropertyInteraction(PreviewElementViewModel element, string property, object? newValue)
    {
        var binding = _project.Document.Bindings
            .FirstOrDefault(b => b.ComponentId == element.Id && b.Property == property);

        if (binding is not null)
        {
            _runtime.RuntimeStates[binding.State] = newValue;
            Log.Insert(0, $"State '{binding.State}' = {newValue}");
        }
    }

    public void FireEvent(PreviewElementViewModel element, string eventName)
    {
        _runtime.FireEvent(element.Model, eventName);

        // Sync log from the runtime (which appends to its own log).
        // In a full implementation the runtime would notify via events;
        // for now we merge.
        foreach (var entry in _runtime.Log.Where(e => !Log.Contains(e)))
        {
            Log.Insert(0, entry);
        }
        _runtime.Log.Clear();
    }
}
