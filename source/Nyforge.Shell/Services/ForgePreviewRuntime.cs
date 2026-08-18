using Nyforge.Core.Nui;
using Nyforge.Core.Project;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Services;

/// <summary>
/// The Forge preview's honest stand-in <see cref="INuiRuntime"/> — wraps
/// the existing <see cref="BehaviorDispatcher"/> and provides the event
/// dispatch + binding application that <see cref="PreviewViewModel"/>
/// currently does inline. A future <c>NyrqisRuntime</c> will provide the
/// real shell rendering; this one renders into Forge's own Avalonia
/// preview. Both implement the same interface so application logic is
/// host-independent (NFC-001 §5.1).
/// </summary>
public sealed class ForgePreviewRuntime : INuiRuntime
{
    public IDictionary<string, object?> RuntimeStates => _states;
    private readonly Dictionary<string, object?> _states;
    public IList<string> Log { get; }

    private readonly BehaviorDispatcher _dispatcher;
    private readonly NyforgeProject _project;

    /// <summary>Callback invoked when a binding changes a preview
    /// element's property — the PreviewViewModel hooks this to update
    /// its live element VMs.</summary>
    public event Action<string, string, object?>? PropertyChanged;

    /// <summary>Callback invoked when a window should close — the
    /// PreviewViewModel hooks this for CloseRequested.</summary>
    public event Action<string>? CloseRequested;

    public ForgePreviewRuntime(
        NyforgeProject project,
        ThemeManager themeManager)
    {
        _project = project;
        _states = project.Document.FlattenedStates();
        Log = new List<string>();

        _dispatcher = new BehaviorDispatcher(
            themeManager,
            _states,
            message => Log.Insert(0, message),
            windowId => CloseRequested?.Invoke(windowId));
    }

    public void FireEvent(NuiComponent component, string eventName)
    {
        if (!component.Events.TryGetValue(eventName, out var behaviorId) || behaviorId is null) return;

        var behavior = _project.Document.Behaviors.FirstOrDefault(b => b.Id == behaviorId);
        if (behavior is null)
        {
            Log.Insert(0, $"Behavior '{behaviorId}' not found.");
            return;
        }

        _dispatcher.Fire(behavior);
    }

    public void ApplyBinding(NuiBinding binding)
    {
        if (!RuntimeStates.TryGetValue(binding.State, out var value)) return;
        PropertyChanged?.Invoke(binding.ComponentId, binding.Property, value);
    }
}
