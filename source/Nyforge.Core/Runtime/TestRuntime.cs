using Nyforge.Core.Nui;

namespace Nyforge.Core.Runtime;

/// <summary>
/// A test-only <see cref="INuiRuntime"/> that records every call for
/// verification — the "third runtime" from doc #8's original architecture
/// (alongside <see cref="ForgePreviewRuntime"/> and the future real
/// Nyrqis runtime). Lives in Nyforge.Core so tests can exercise behavior
/// dispatch and binding application without depending on Avalonia.
/// </summary>
public sealed class TestRuntime : INuiRuntime
{
    public IDictionary<string, object?> RuntimeStates { get; }
    public IList<string> Log { get; } = new List<string>();

    /// <summary>Every event that was fired, in order: (componentId, eventName).</summary>
    public List<(string ComponentId, string EventName)> FiredEvents { get; } = new();

    /// <summary>Every binding that was applied, in order.</summary>
    public List<NuiBinding> AppliedBindings { get; } = new();

    /// <summary>When true, all behavior conditions evaluate to true
    /// (useful for testing action dispatch without setting up state).</summary>
    public bool ForceAllConditions { get; set; }

    public TestRuntime(NuiDocument? document = null)
    {
        RuntimeStates = document is not null
            ? new Dictionary<string, object?>(document.FlattenedStates())
            : new Dictionary<string, object?>();
    }

    public void FireEvent(NuiComponent component, string eventName)
    {
        FiredEvents.Add((component.Id, eventName));
        Log.Add($"FireEvent({component.Id}, {eventName})");
    }

    public void ApplyBinding(NuiBinding binding)
    {
        AppliedBindings.Add(binding);
        if (RuntimeStates.TryGetValue(binding.State, out var value))
        {
            Log.Add($"ApplyBinding({binding.ComponentId}.{binding.Property} ← {binding.State}={value})");
        }
        else
        {
            Log.Add($"ApplyBinding({binding.ComponentId}.{binding.Property} ← {binding.State}=null)");
        }
    }
}
