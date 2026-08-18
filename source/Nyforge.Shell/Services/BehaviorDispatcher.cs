using Nyforge.Core.Nui;
using Nyforge.Core.Runtime;

namespace Nyforge.Shell.Services;

/// <summary>
/// Host-specific action execution for the Preview stand-in. This is
/// deliberately NOT in Nyforge.Core (NFC-001 §5.1) — what
/// "Nyrqis.Theme.Set" *means* is specific to Forge's own Avalonia-based
/// preview today, and will mean something different (and be implemented
/// elsewhere) once a real Nyrqis UI Runtime exists. BehaviorEvaluator
/// (Nyforge.Core.Runtime) supplies the condition-checking logic that IS
/// host-independent; this class supplies the parts that aren't.
/// </summary>
public sealed class BehaviorDispatcher
{
    private readonly ThemeManager _themeManager;
    private readonly Dictionary<string, object?> _runtimeStates;
    private readonly Action<string> _log;
    private readonly Action<string> _closeWindow;

    public BehaviorDispatcher(
        ThemeManager themeManager,
        Dictionary<string, object?> runtimeStates,
        Action<string> log,
        Action<string> closeWindow)
    {
        _themeManager = themeManager;
        _runtimeStates = runtimeStates;
        _log = log;
        _closeWindow = closeWindow;
    }

    /// <summary>
    /// Fires the behavior an event pointed at, if its condition (evaluated
    /// against the live runtime state snapshot, not the saved document) holds.
    /// A behavior carries exactly one of a single <c>action</c> or an
    /// <c>actions</c> chain (NUI-SCHEMA §7.3); the chain runs in order.
    /// </summary>
    public void Fire(NuiBehavior behavior)
    {
        var actions = behavior.Actions is { Count: > 0 } chain
            ? chain
            : behavior.Action is { } single ? new List<NuiAction> { single } : new();
        var first = actions.FirstOrDefault();

        if (!BehaviorEvaluator.Evaluate(behavior.Condition, _runtimeStates))
        {
            if (first is not null)
            {
                _log($"Condition not met, skipped: {first.Target}.{first.Name}");
            }
            return;
        }

        // Resolve "$state:key" / "$expr:" argument placeholders against
        // the live runtime state before executing — see
        // ActionArgumentResolver's doc comment and NUI-SCHEMA.md §7's
        // "Expression-valued arguments" subsection. Resolution happens
        // per step, so every branch below (system actions, component
        // actions) sees already-resolved values.
        foreach (var action in actions)
        {
            var resolvedArguments = ActionArgumentResolver.Resolve(action.Arguments, _runtimeStates);
            Execute(action, resolvedArguments);
        }
    }

    private void Execute(NuiAction action, IReadOnlyDictionary<string, object?> resolvedArguments)
    {
        if (action.Target == "System")
        {
            ExecuteSystemAction(action, resolvedArguments);
            return;
        }

        // Component-instance action. v0.3 implements the ones the preview
        // can meaningfully honor; everything else is logged rather than
        // silently ignored, per NFM-000 §2.1 ("the canvas is truthful").
        switch (action.Name)
        {
            case "Close" when action.Target != "System":
                _closeWindow(action.Target);
                break;
            default:
                _log($"(unimplemented in preview) {action.Target}.{action.Name}{DescribeArguments(resolvedArguments)}");
                break;
        }
    }

    private void ExecuteSystemAction(NuiAction action, IReadOnlyDictionary<string, object?> resolvedArguments)
    {
        if (!NuiSystemActions.TryGet(action.Name, out _))
        {
            _log($"Unknown system action '{action.Name}' — refusing to guess what it means.");
            return;
        }

        switch (action.Name)
        {
            case "Nyrqis.Theme.Set":
                if (!resolvedArguments.TryGetValue("theme", out var themeObj) || themeObj is not string theme)
                {
                    _log($"Nyrqis.Theme.Set: 'theme' argument missing or not a string{DescribeArguments(resolvedArguments)}.");
                    break;
                }
                if (!ThemeManager.AvailableThemes.ContainsKey(theme))
                {
                    _log($"Nyrqis.Theme.Set: unknown theme '{theme}' — registered themes are {string.Join(", ", ThemeManager.AvailableThemes.Keys)}.");
                    break;
                }
                _themeManager.SetTheme(theme);
                _log($"Theme set to {theme}.");
                break;

            case "Nyrqis.Settings.Commit":
                _log("Settings committed.");
                break;

            case "Nyrqis.Window.Close":
                if (resolvedArguments.TryGetValue("windowId", out var winId) && winId is string wid)
                {
                    _closeWindow(wid);
                }
                else
                {
                    _log("Nyrqis.Window.Close: 'windowId' argument missing or not a string.");
                }
                break;

            case "Nyrqis.Dialog.Open":
            case "Nyrqis.Dialog.Close":
            case "Nyrqis.Notification.Show":
                // No modal/notification surface in the v0.3 preview stand-in yet —
                // logged (with resolved arguments, so $state: substitution is
                // still visible/verifiable here) rather than silently dropped.
                // See engineering/ROADMAP.md.
                _log($"(unimplemented in preview) {action.Name}{DescribeArguments(resolvedArguments)}");
                break;
        }
    }

    private static string DescribeArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        if (arguments.Count == 0) return string.Empty;
        var parts = arguments.Select(kv => $"{kv.Key}={kv.Value}");
        return $" ({string.Join(", ", parts)})";
    }
}
