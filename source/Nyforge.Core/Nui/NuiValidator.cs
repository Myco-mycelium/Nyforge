namespace Nyforge.Core.Nui;

/// <summary>
/// Severity of a design-time validation finding. Errors block Preview
/// (and should block export when exporters exist); warnings and infos
/// are surfaced but never block.
/// </summary>
public enum NuiIssueSeverity
{
    Error,
    Warning,
    Info
}

/// <summary>
/// One validation finding. Codes are stable identifiers (ER-NUI-XXX /
/// WN-NUI-XXX / IN-NUI-XXX) so tests and the eventual lint UI can pin
/// specific checks, and so a finding's meaning survives message rewording.
/// </summary>
public sealed record NuiIssue(
    string Code,
    NuiIssueSeverity Severity,
    string Message,
    string? ComponentId = null,
    string? BehaviorId = null,
    string? ScreenId = null);

/// <summary>
/// The result of <see cref="NuiValidator.Validate"/>. Convenience
/// accessors for the common "does this document ship?" question.
/// </summary>
public sealed class NuiValidationResult
{
    public IReadOnlyList<NuiIssue> Issues { get; }

    public NuiValidationResult(IReadOnlyList<NuiIssue> issues)
    {
        Issues = issues;
    }

    public bool HasErrors => Issues.Any(i => i.Severity == NuiIssueSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == NuiIssueSeverity.Warning);

    public IEnumerable<NuiIssue> Errors =>
        Issues.Where(i => i.Severity == NuiIssueSeverity.Error);
    public IEnumerable<NuiIssue> Warnings =>
        Issues.Where(i => i.Severity == NuiIssueSeverity.Warning);
    public IEnumerable<NuiIssue> Infos =>
        Issues.Where(i => i.Severity == NuiIssueSeverity.Info);
}

/// <summary>
/// Design-time validator/linter for NUI documents — the "check before
/// Preview" step from the original architecture review. It mirrors the
/// Nyrqis import gate's hard rules at design time (so a screen that
/// fails here would fail the runtime gate too) and adds the design-only
/// findings the gate can't know about: duplicate ids are a warning here
/// (the gate rejects them outright), overflow and missing resources are
/// layout/asset concerns, and the reusable-instance candidate is pure
/// advice.
///
/// Run from the editor before Preview (MainWindowViewModel.CreatePreview
/// blocks on <see cref="NuiValidationResult.HasErrors"/>) and in CI over
/// every example fixture.
/// </summary>
public static class NuiValidator
{
    private static readonly Dictionary<string, ComponentContract> ContractsByType =
        ComponentContracts.All.ToDictionary(c => c.Type, StringComparer.Ordinal);

    private static readonly Dictionary<string, SystemActionContract> SystemActionsByName =
        NuiSystemActions.All.ToDictionary(a => a.Name, StringComparer.Ordinal);

    /// <summary>
    /// Validate a document. <paramref name="projectDirectory"/>, when
    /// given, is the directory relative asset references (Image
    /// <c>source</c>) are resolved against for the missing-resource
    /// warning.
    /// </summary>
    public static NuiValidationResult Validate(NuiDocument document, string? projectDirectory = null)
    {
        var issues = new List<NuiIssue>();

        var allIds = new HashSet<string>(StringComparer.Ordinal);
        var masterIds = new HashSet<string>(
            document.ReusableComponents.Select(m => m.Id), StringComparer.Ordinal);

        // ---- component trees -------------------------------------------------
        foreach (var screen in document.Screens)
        {
            Walk(screen.Root, screen.Id, null, null, document, masterIds, allIds,
                 issues, projectDirectory);
        }

        // ---- behavior / binding / action references -------------------------
        var behaviorIds = new HashSet<string>(
            document.Behaviors.Select(b => b.Id), StringComparer.Ordinal);
        foreach (var behavior in document.Behaviors)
        {
            ValidateBehavior(behavior, document, allIds, behaviorIds, issues);
        }

        foreach (var binding in document.Bindings)
        {
            ValidateBinding(binding, document, allIds, issues);
        }

        // ---- reusable masters -------------------------------------------------
        foreach (var master in document.ReusableComponents)
        {
            if (string.IsNullOrWhiteSpace(master.Id))
            {
                issues.Add(new NuiIssue("WN-NUI-003", NuiIssueSeverity.Warning,
                    "Reusable master has an empty id.", ComponentId: master.Id));
            }
            ValidateComponentNode(master, null, document, masterIds, issues,
                isMaster: true);
        }

        // ---- unused behaviors -------------------------------------------------
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var screen in document.Screens)
            CollectReferencedBehaviors(screen.Root, referenced);
        foreach (var id in behaviorIds)
        {
            if (!referenced.Contains(id))
            {
                issues.Add(new NuiIssue("WN-NUI-006", NuiIssueSeverity.Warning,
                    $"Behavior '{id}' is never referenced by any component event.",
                    BehaviorId: id));
            }
        }

        // ---- reusable-instance candidates (advice, not a problem) ------------
        foreach (var screen in document.Screens)
            CollectReuseCandidates(screen.Root, new HashSet<string>(StringComparer.Ordinal), issues);

        return new NuiValidationResult(issues);
    }

    // -------------------------------------------------------------------------

    private static void Walk(
        NuiComponent node,
        string? screenId,
        NuiComponent? parent,
        string? parentScreenId,
        NuiDocument document,
        HashSet<string> masterIds,
        HashSet<string> allIds,
        List<NuiIssue> issues,
        string? projectDirectory)
    {
        if (!string.IsNullOrEmpty(node.Id) && !allIds.Add(node.Id))
        {
            issues.Add(new NuiIssue("WN-NUI-001", NuiIssueSeverity.Warning,
                $"Duplicate component id '{node.Id}' — ids must be unique within " +
                "the document.",
                ComponentId: node.Id));
        }

        ValidateComponentNode(node, parent, document, masterIds, issues, isMaster: false);
        CheckOverflow(node, parent, screenId, issues);
        CheckMissingImageSource(node, projectDirectory, issues);

        foreach (var child in node.Children)
        {
            Walk(child, screenId, node, parentScreenId, document, masterIds,
                 allIds, issues, projectDirectory);
        }
    }

    private static void ValidateComponentNode(
        NuiComponent node,
        NuiComponent? parent,
        NuiDocument document,
        HashSet<string> masterIds,
        List<NuiIssue> issues,
        bool isMaster)
    {
        var where = isMaster ? $"master '{node.Id}'" : $"component '{node.Id}'";

        if (string.IsNullOrWhiteSpace(node.Id))
        {
            issues.Add(new NuiIssue("WN-NUI-003", NuiIssueSeverity.Warning,
                "Component has an empty id.", ComponentId: node.Id));
        }

        // Reusable instance? Its contract is the master's.
        if (!string.IsNullOrEmpty(node.ComponentRef))
        {
            if (node.ComponentRef != null && !masterIds.Contains(node.ComponentRef))
            {
                issues.Add(new NuiIssue("ER-NUI-011", NuiIssueSeverity.Error,
                    $"{where} references reusable component '{node.ComponentRef}', " +
                    "which is not defined in the document's components[] section.",
                    ComponentId: node.Id));
            }
            if (!string.IsNullOrEmpty(node.Type))
            {
                issues.Add(new NuiIssue("ER-NUI-012", NuiIssueSeverity.Error,
                    $"{where} is a reusable instance (componentRef set) but also " +
                    "declares its own type — instances omit type; the master's " +
                    "type governs.",
                    ComponentId: node.Id));
            }
            var master = document.ReusableComponents
                .FirstOrDefault(m => m.Id == node.ComponentRef);
            if (master != null)
            {
                var masterContract = LookupContract(master.Type);
                foreach (var (key, _) in node.Overrides)
                {
                    if (masterContract != null &&
                        !masterContract.Properties.Contains(key, StringComparer.Ordinal))
                    {
                        issues.Add(new NuiIssue("ER-NUI-013", NuiIssueSeverity.Error,
                            $"{where} override '{key}' is not a property of the " +
                            $"'{master.Type}' contract.",
                            ComponentId: node.Id));
                    }
                }
                ValidateEvents(node, masterContract, master.Type, document, issues,
                    isMaster ? "master" : "instance", isMaster);
            }
            return;
        }

        var contract = LookupContract(node.Type);
        if (contract is null)
        {
            issues.Add(new NuiIssue("ER-NUI-001", NuiIssueSeverity.Error,
                $"{where} has unknown type '{node.Type}'.", ComponentId: node.Id));
            return;
        }

        foreach (var (key, _) in node.Properties)
        {
            if (!contract.Properties.Contains(key, StringComparer.Ordinal))
            {
                issues.Add(new NuiIssue("ER-NUI-002", NuiIssueSeverity.Error,
                    $"{where} property '{key}' is not in the '{node.Type}' contract.",
                    ComponentId: node.Id));
            }
        }

        ValidateEvents(node, contract, node.Type, document, issues,
            "component", isMaster);
    }

    private static void ValidateEvents(
        NuiComponent node,
        ComponentContract? contract,
        string typeName,
        NuiDocument document,
        List<NuiIssue> issues,
        string what,
        bool isMaster)
    {
        var where = isMaster ? $"master '{node.Id}'" : $"component '{node.Id}'";
        var behaviorIds = new HashSet<string>(
            document.Behaviors.Select(b => b.Id), StringComparer.Ordinal);

        foreach (var (eventName, behaviorId) in node.Events)
        {
            if (contract != null && !contract.Events.Contains(eventName, StringComparer.Ordinal))
            {
                issues.Add(new NuiIssue("ER-NUI-003", NuiIssueSeverity.Error,
                    $"{where} event '{eventName}' is not in the '{typeName}' contract.",
                    ComponentId: node.Id));
            }
            if (behaviorId is not null && !behaviorIds.Contains(behaviorId))
            {
                issues.Add(new NuiIssue("ER-NUI-004", NuiIssueSeverity.Error,
                    $"{where} event '{eventName}' references behavior '{behaviorId}' " +
                    "which does not exist.",
                    ComponentId: node.Id, BehaviorId: behaviorId));
            }
        }
    }

    private static void ValidateBehavior(
        NuiBehavior behavior,
        NuiDocument document,
        HashSet<string> allIds,
        HashSet<string> behaviorIds,
        List<NuiIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(behavior.Id))
        {
            issues.Add(new NuiIssue("WN-NUI-002", NuiIssueSeverity.Warning,
                "Behavior has an empty id.", BehaviorId: behavior.Id));
        }

        if (behavior.Condition is { } condition &&
            !document.States.ContainsKey(condition.State))
        {
            issues.Add(new NuiIssue("ER-NUI-005", NuiIssueSeverity.Error,
                $"Behavior '{behavior.Id}' condition references state " +
                $"'{condition.State}' which no longer exists.",
                BehaviorId: behavior.Id));
        }

        var action = behavior.Action;
        if (action.Target == "System")
        {
            if (!SystemActionsByName.TryGetValue(action.Name, out var sys))
            {
                issues.Add(new NuiIssue("ER-NUI-007", NuiIssueSeverity.Error,
                    $"Behavior '{behavior.Id}' references unknown system action " +
                    $"'{action.Name}'.",
                    BehaviorId: behavior.Id));
            }
            else
            {
                foreach (var (key, _) in action.Arguments)
                {
                    if (!sys.ArgumentNames.Contains(key, StringComparer.Ordinal))
                    {
                        issues.Add(new NuiIssue("ER-NUI-008", NuiIssueSeverity.Error,
                            $"Behavior '{behavior.Id}' passes argument '{key}' to " +
                            $"'{action.Name}' which does not accept it.",
                            BehaviorId: behavior.Id));
                    }
                }
            }
        }
        else if (action.Target != "System")
        {
            if (!allIds.Contains(action.Target))
            {
                issues.Add(new NuiIssue("ER-NUI-006", NuiIssueSeverity.Error,
                    $"Behavior '{behavior.Id}' action targets component " +
                    $"'{action.Target}' which does not exist.",
                    BehaviorId: behavior.Id));
            }
            else
            {
                var targetContract = FindContractForId(action.Target, document);
                if (targetContract != null && !string.IsNullOrEmpty(action.Name) &&
                    targetContract.Actions.Count > 0 &&
                    !targetContract.Actions.Contains(action.Name, StringComparer.Ordinal))
                {
                    issues.Add(new NuiIssue("ER-NUI-015", NuiIssueSeverity.Error,
                        $"Behavior '{behavior.Id}' targets '{action.Target}' with " +
                        $"action '{action.Name}' which is not in the " +
                        $"'{targetContract.Type}' contract.",
                        BehaviorId: behavior.Id));
                }
            }
        }
    }

    private static void ValidateBinding(
        NuiBinding binding,
        NuiDocument document,
        HashSet<string> allIds,
        List<NuiIssue> issues)
    {
        if (!allIds.Contains(binding.ComponentId))
        {
            issues.Add(new NuiIssue("ER-NUI-009", NuiIssueSeverity.Error,
                $"Binding references component '{binding.ComponentId}' which does not exist."));
        }
        if (!document.States.ContainsKey(binding.State))
        {
            issues.Add(new NuiIssue("ER-NUI-010", NuiIssueSeverity.Error,
                $"Binding on '{binding.ComponentId}' references state " +
                $"'{binding.State}' which does not exist."));
        }
        var contract = FindContractForId(binding.ComponentId, document);
        if (contract != null && !contract.Properties.Contains(binding.Property, StringComparer.Ordinal))
        {
            issues.Add(new NuiIssue("IN-NUI-002", NuiIssueSeverity.Info,
                $"Binding on '{binding.ComponentId}' binds property " +
                $"'{binding.Property}' which is not in the '{contract.Type}' contract."));
        }
    }

    private static void CheckOverflow(
        NuiComponent node,
        NuiComponent? parent,
        string? screenId,
        List<NuiIssue> issues)
    {
        if (parent is null) return;
        var p = parent.Layout;
        if (p.Width <= 0 || p.Height <= 0) return;
        var c = node.Layout;
        if (c.X + c.Width > p.Width + 0.5)
        {
            issues.Add(new NuiIssue("WN-NUI-004", NuiIssueSeverity.Warning,
                $"Component '{node.Id}' overflows its parent: right edge " +
                $"{c.X + c.Width:0} > parent width {p.Width:0}.",
                ComponentId: node.Id, ScreenId: screenId));
        }
        if (c.Y + c.Height > p.Height + 0.5)
        {
            issues.Add(new NuiIssue("WN-NUI-004", NuiIssueSeverity.Warning,
                $"Component '{node.Id}' overflows its parent: bottom edge " +
                $"{c.Y + c.Height:0} > parent height {p.Height:0}.",
                ComponentId: node.Id, ScreenId: screenId));
        }
    }

    private static void CheckMissingImageSource(
        NuiComponent node,
        string? projectDirectory,
        List<NuiIssue> issues)
    {
        if (node.Type != "Image") return;
        var source = node.Properties.TryGetValue("source", out var raw) ? raw as string : null;
        if (string.IsNullOrWhiteSpace(source))
        {
            issues.Add(new NuiIssue("WN-NUI-005", NuiIssueSeverity.Warning,
                $"Image '{node.Id}' has no source.",
                ComponentId: node.Id));
            return;
        }
        if (projectDirectory is not null &&
            !source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !source.StartsWith("$localize:", StringComparison.Ordinal) &&
            !File.Exists(Path.Combine(projectDirectory, source)))
        {
            issues.Add(new NuiIssue("WN-NUI-005", NuiIssueSeverity.Warning,
                $"Image '{node.Id}' references '{source}' which does not exist " +
                "relative to the project directory.",
                ComponentId: node.Id));
        }
    }

    private static void CollectReferencedBehaviors(NuiComponent node, HashSet<string> referenced)
    {
        foreach (var (_, behaviorId) in node.Events)
        {
            if (behaviorId is not null) referenced.Add(behaviorId);
        }
        foreach (var child in node.Children)
            CollectReferencedBehaviors(child, referenced);
    }

    private static void CollectReuseCandidates(
        NuiComponent node,
        HashSet<string> seenSignatures,
        List<NuiIssue> issues)
    {
        if (string.IsNullOrEmpty(node.ComponentRef) && !string.IsNullOrEmpty(node.Type))
        {
            var signature = node.Type + "{" +
                string.Join(",", node.Properties.OrderBy(kv => kv.Key)
                    .Select(kv => kv.Key + "=" + (kv.Value ?? "null"))) + "}";
            if (!seenSignatures.Add(signature))
            {
                issues.Add(new NuiIssue("IN-NUI-001", NuiIssueSeverity.Info,
                    $"Component '{node.Id}' duplicates another '{node.Type}' with " +
                    "the same properties — consider defining it once as a " +
                    "reusable master and placing componentRef instances.",
                    ComponentId: node.Id));
            }
        }
        foreach (var child in node.Children)
            CollectReuseCandidates(child, seenSignatures, issues);
    }

    private static ComponentContract? LookupContract(string type) =>
        ContractsByType.TryGetValue(type, out var c) ? c : null;

    private static ComponentContract? FindContractForId(string id, NuiDocument document)
    {
        foreach (var screen in document.Screens)
        {
            var found = FindInTree(screen.Root, id);
            if (found != null) return LookupContract(found.Type);
        }
        var master = document.ReusableComponents.FirstOrDefault(m => m.Id == id);
        if (master != null) return LookupContract(master.Type);
        return null;
    }

    private static NuiComponent? FindInTree(NuiComponent node, string id)
    {
        if (node.Id == id) return node;
        foreach (var child in node.Children)
        {
            var found = FindInTree(child, id);
            if (found != null) return found;
        }
        return null;
    }
}
