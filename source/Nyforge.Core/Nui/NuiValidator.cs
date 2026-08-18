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

        // ---- animations (NUI-SCHEMA §8.3) ------------------------------------
        // Validated before behaviors so Nyrqis.Animation.Play references
        // can be checked against the declared ids.
        var animationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var animation in document.Animations)
        {
            if (string.IsNullOrWhiteSpace(animation.Id))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    "Animation has an empty id."));
                continue;
            }
            if (!animationIds.Add(animation.Id))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Duplicate animation id '{animation.Id}'."));
            }
            if (string.IsNullOrWhiteSpace(animation.Property))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Animation '{animation.Id}' must declare a property."));
            }
            if (!string.IsNullOrEmpty(animation.Target) &&
                !allIds.Contains(animation.Target))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Animation '{animation.Id}' targets component " +
                    $"'{animation.Target}' which does not exist."));
            }
            foreach (var (name, value) in new[]
            {
                ("duration", animation.Duration),
                ("delay", animation.Delay),
                ("repeat", animation.Repeat),
            })
            {
                if (value < 0)
                {
                    issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                        $"Animation '{animation.Id}' '{name}' must be non-negative."));
                }
            }
            if (animation.Easing is not ("linear" or "ease-in" or "ease-out"
                or "ease-in-out" or "steps"))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Animation '{animation.Id}' easing '{animation.Easing}' is not " +
                    "one of linear / ease-in / ease-out / ease-in-out / steps."));
            }
            if (animation.Direction is not ("forward" or "reverse" or "alternate"))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Animation '{animation.Id}' direction '{animation.Direction}' is " +
                    "not one of forward / reverse / alternate."));
            }
            // Keyframes (NUI-SCHEMA §8.3): offsets in [0, 1], strictly
            // increasing, each with a value (number/string/boolean).
            double prevOffset = double.NaN;
            for (var i = 0; i < animation.Keyframes.Count; i++)
            {
                var keyframe = animation.Keyframes[i];
                if (keyframe.Offset < 0 || keyframe.Offset > 1)
                {
                    issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                        $"Animation '{animation.Id}' keyframe {i} 'offset' must be " +
                        "a number in [0, 1]."));
                }
                else if (i > 0 && keyframe.Offset <= prevOffset)
                {
                    issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                        $"Animation '{animation.Id}' keyframe {i} 'offset' must be " +
                        "greater than the previous offset."));
                }
                else
                {
                    prevOffset = keyframe.Offset;
                }
                if (keyframe.Value is null ||
                    keyframe.Value is System.Collections.IDictionary ||
                    keyframe.Value is System.Collections.IList)
                {
                    issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                        $"Animation '{animation.Id}' keyframe {i} 'value' must be " +
                        "a number, string, or boolean."));
                }
            }
        }

        // ---- behavior / binding / action references -------------------------
        var behaviorIds = new HashSet<string>(
            document.Behaviors.Select(b => b.Id), StringComparer.Ordinal);
        foreach (var behavior in document.Behaviors)
        {
            ValidateBehavior(behavior, document, allIds, behaviorIds, animationIds, issues);
        }

        foreach (var binding in document.Bindings)
        {
            ValidateBinding(binding, document, allIds, issues);
        }

        // ---- stateScopes (NUI-SCHEMA §8.4) ----------------------------------
        // Scopes must be one of the five named scope kinds and each must
        // be an object table — mirroring the Nyrqis import gate fail-closed.
        foreach (var (scope, table) in document.StateScopes)
        {
            if (scope is not ("global" or "screen" or "component"
                or "session" or "persistent"))
            {
                issues.Add(new NuiIssue("ER-NUI-023", NuiIssueSeverity.Error,
                    $"stateScopes: unknown scope '{scope}'."));
            }
            if (table is null)
            {
                issues.Add(new NuiIssue("ER-NUI-023", NuiIssueSeverity.Error,
                    $"stateScopes: scope '{scope}' must be an object."));
            }
        }

        // ---- resources (NUI-SCHEMA §8.2) ------------------------------------
        var assetIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in document.Resources.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                issues.Add(new NuiIssue("ER-NUI-020", NuiIssueSeverity.Error,
                    "Resource has an empty id."));
                continue;
            }
            if (!assetIds.Add(asset.Id))
            {
                issues.Add(new NuiIssue("ER-NUI-020", NuiIssueSeverity.Error,
                    $"Duplicate resource id '{asset.Id}'."));
            }
            if (projectDirectory is not null && !string.IsNullOrEmpty(asset.Path) &&
                !File.Exists(Path.Combine(projectDirectory, asset.Path)))
            {
                issues.Add(new NuiIssue("WN-NUI-007", NuiIssueSeverity.Warning,
                    $"Resource '{asset.Id}' references '{asset.Path}' which does " +
                    "not exist relative to the project directory."));
            }
        }
        var seenHashes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var asset in document.Resources.Assets)
        {
            if (asset.Sha256 is null) continue;
            if (!seenHashes.Add(asset.Sha256))
            {
                issues.Add(new NuiIssue("WN-NUI-008", NuiIssueSeverity.Warning,
                    $"Resource '{asset.Id}' has the same content hash as another " +
                    "resource — consider deduplicating."));
            }
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
        CheckLayoutConstraints(node, issues);
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
                foreach (var (_, value) in node.Overrides)
                {
                    CheckLocalize(value, document, issues, $"{where} override");
                    CheckAssetRef(value, document, issues, $"{where} override");
                    CheckExprRef(value, document, issues, $"{where} override");
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
        foreach (var (_, value) in node.Properties)
        {
            CheckLocalize(value, document, issues, $"{where} property");
            CheckAssetRef(value, document, issues, $"{where} property");
            CheckExprRef(value, document, issues, $"{where} property");
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
        HashSet<string> animationIds,
        List<NuiIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(behavior.Id))
        {
            issues.Add(new NuiIssue("WN-NUI-002", NuiIssueSeverity.Warning,
                "Behavior has an empty id.", BehaviorId: behavior.Id));
        }

        if (behavior.Condition is { } condition)
        {
            if (!string.IsNullOrEmpty(condition.Expression))
            {
                // Expression conditions (NUI-SCHEMA §7.2) supersede the
                // legacy equality form and must pass the same checks as
                // the Nyrqis import gate. State references resolve against
                // the flat section AND every declared scope (NUI-SCHEMA §8.4).
                var states = ScopedStateKeys(document);
                var problem = NExpr.TryValidate(condition.Expression, states);
                if (problem is not null)
                {
                    issues.Add(new NuiIssue("ER-NUI-021", NuiIssueSeverity.Error,
                        $"Behavior '{behavior.Id}' condition expression: {problem}.",
                        BehaviorId: behavior.Id));
                }
            }
            else if (!document.IsStateKnown(condition.State))
            {
                issues.Add(new NuiIssue("ER-NUI-005", NuiIssueSeverity.Error,
                    $"Behavior '{behavior.Id}' condition references state " +
                    $"'{condition.State}' which no longer exists.",
                    BehaviorId: behavior.Id));
            }
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
        foreach (var (_, value) in action.Arguments)
        {
            CheckLocalize(value, document, issues, $"behavior '{behavior.Id}' argument");
            CheckExprRef(value, document, issues, $"behavior '{behavior.Id}' argument");
        }
        // Animations (NUI-SCHEMA §8.3): the reference must name a
        // declared animation — fail-closed like the Nyrqis import gate.
        if (action.Name == "Nyrqis.Animation.Play")
        {
            var animationId = action.Arguments.TryGetValue("animation", out var raw)
                ? raw?.ToString()
                : null;
            if (string.IsNullOrEmpty(animationId) ||
                !animationIds.Contains(animationId))
            {
                issues.Add(new NuiIssue("ER-NUI-022", NuiIssueSeverity.Error,
                    $"Behavior '{behavior.Id}' plays animation " +
                    $"'{animationId}' which is not declared in the " +
                    "animations section.",
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
        if (!document.IsStateKnown(binding.State))
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

    private static void CheckAssetRef(object? value, NuiDocument document,
        List<NuiIssue> issues, string where)
    {
        // $asset:id references must name a declared resource (fail-closed,
        // mirroring the Nyrqis import gate).
        if (value is not string text || !text.Contains("$asset:", StringComparison.Ordinal))
            return;
        var assets = document.Resources.Assets;
        if (assets.Count == 0)
        {
            issues.Add(new NuiIssue("ER-NUI-020", NuiIssueSeverity.Error,
                $"{where}: a '$asset:' reference requires a 'resources' " +
                "section with an 'assets' list."));
            return;
        }
        var ids = new HashSet<string>(assets.Select(a => a.Id), StringComparer.Ordinal);
        foreach (var key in AssetReferences(text))
        {
            if (!ids.Contains(key))
            {
                issues.Add(new NuiIssue("ER-NUI-020", NuiIssueSeverity.Error,
                    $"{where}: asset '{key}' is not declared in resources."));
            }
        }
    }

    private static IEnumerable<string> AssetReferences(string text)
    {
        const string prefix = "$asset:";
        var rest = text;
        while (true)
        {
            var pos = rest.IndexOf(prefix, StringComparison.Ordinal);
            if (pos < 0) yield break;
            rest = rest[(pos + prefix.Length)..];
            var key = new string(rest.TakeWhile(c =>
                char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-').ToArray());
            if (key.Length > 0) yield return key;
        }
    }

    private static void CheckLocalize(object? value, NuiDocument document,
        List<NuiIssue> issues, string where)
    {
        // $localize:key references must exist in the ACTIVE locale's
        // table (fail-closed, mirroring the Nyrqis import gate). A
        // reference with no locales section is an error too.
        if (value is not string text || !Localize.HasReference(text)) return;
        var locales = document.Locales;
        var active = locales.Active;
        if (string.IsNullOrEmpty(active) || locales.Tables.Count == 0)
        {
            issues.Add(new NuiIssue("ER-NUI-019", NuiIssueSeverity.Error,
                $"{where}: a '$localize:' reference requires a 'locales' " +
                "section with an 'active' locale."));
            return;
        }
        if (!locales.Tables.TryGetValue(active, out var table))
        {
            issues.Add(new NuiIssue("ER-NUI-019", NuiIssueSeverity.Error,
                $"{where}: locale '{active}' has no table."));
            return;
        }
        foreach (var key in Localize.References(text))
        {
            if (!table.ContainsKey(key))
            {
                issues.Add(new NuiIssue("ER-NUI-019", NuiIssueSeverity.Error,
                    $"{where}: localize key '{key}' not in locale '{active}'."));
            }
        }
    }

    private static HashSet<string> ScopedStateKeys(NuiDocument document)
    {
        // Every state reference the expression language may use: flat
        // keys plus each declared scope's entries under their dotted
        // names (NUI-SCHEMA §8.4) — mirrors the floor's
        // `_scoped_state_keys(doc) | set(doc.states)`.
        var keys = new HashSet<string>(document.States.Keys, StringComparer.Ordinal);
        foreach (var (scope, table) in document.StateScopes)
        {
            if (table is null) continue;
            foreach (var key in table.Keys)
            {
                keys.Add($"{scope}.{key}");
            }
        }
        return keys;
    }

    private static void CheckExprRef(object? value, NuiDocument document,
        List<NuiIssue> issues, string where)
    {
        // Whole-string `$expr:` values (NUI-SCHEMA §7.2) must parse, use
        // only known functions with correct arity, and reference only
        // declared states — mirroring the Nyrqis import gate fail-closed.
        if (value is not string text ||
            !text.StartsWith("$expr:", StringComparison.Ordinal))
        {
            return; // ordinary value — a literal, $state:, $localize:, ...
        }
        var expression = text["$expr:".Length..];
        var states = ScopedStateKeys(document);
        var problem = NExpr.TryValidate(expression, states);
        if (problem is not null)
        {
            issues.Add(new NuiIssue("ER-NUI-021", NuiIssueSeverity.Error,
                $"{where}: {problem}."));
        }
    }

    private static void CheckLayoutConstraints(NuiComponent node, List<NuiIssue> issues)
    {
        // Mirror the Nyrqis gate's layout-constraint rules (NUI-SCHEMA
        // §4) at design time so a screen fails here before it would fail
        // the runtime import gate.
        var l = node.Layout;
        foreach (var (name, value) in new[]
        {
            ("minWidth", l.MinWidth), ("maxWidth", l.MaxWidth),
            ("minHeight", l.MinHeight), ("maxHeight", l.MaxHeight),
        })
        {
            if (value is < 0)
            {
                issues.Add(new NuiIssue("ER-NUI-016", NuiIssueSeverity.Error,
                    $"Component '{node.Id}' layout '{name}' must be non-negative.",
                    ComponentId: node.Id));
            }
        }
        if (l.MinWidth is { } minW && l.MaxWidth is { } maxW && minW > maxW)
        {
            issues.Add(new NuiIssue("ER-NUI-017", NuiIssueSeverity.Error,
                $"Component '{node.Id}' layout 'minWidth' must be <= 'maxWidth'.",
                ComponentId: node.Id));
        }
        if (l.MinHeight is { } minH && l.MaxHeight is { } maxH && minH > maxH)
        {
            issues.Add(new NuiIssue("ER-NUI-017", NuiIssueSeverity.Error,
                $"Component '{node.Id}' layout 'minHeight' must be <= 'maxHeight'.",
                ComponentId: node.Id));
        }
        if (l.AspectRatio is <= 0)
        {
            issues.Add(new NuiIssue("ER-NUI-018", NuiIssueSeverity.Error,
                $"Component '{node.Id}' layout 'aspectRatio' must be a positive number.",
                ComponentId: node.Id));
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
            !source.StartsWith("$asset:", StringComparison.Ordinal) &&
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
