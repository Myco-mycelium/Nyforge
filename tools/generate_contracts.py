#!/usr/bin/env python3
"""generate_contracts.py — regenerate Nyforge's C# component and
system-action contract tables from the vendored Nyrqis API Registry.

The registry (engineering/registry/nui-api-v1.json) is the single
machine-readable source of truth for the NUI component vocabulary
(NFS-006, NFC-001 §4.3).  NyForge's C# tables derive from it; the
Nyrqis-side Python floor and Rust crate embed the same file.

Usage:
    python tools/generate_contracts.py          # writes both files
    python tools/generate_contracts.py --check  # dry-run; non-zero exit
                                                # if output differs
"""

from __future__ import annotations

import json
import os
import sys
from typing import Any, Dict, List, Tuple

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
NYFORGE_ROOT = os.path.dirname(TOOLS_DIR)
REGISTRY_PATH = os.path.join(
    NYFORGE_ROOT, "engineering", "registry", "nui-api-v1.json")
CONTRACTS_OUT = os.path.join(
    NYFORGE_ROOT, "source", "Nyforge.Core", "Nui", "ComponentContracts.cs")
SYSTEM_ACTIONS_OUT = os.path.join(
    NYFORGE_ROOT, "source", "Nyforge.Core", "Nui", "NuiSystemActions.cs")
PROPERTY_DEFS_OUT = os.path.join(
    NYFORGE_ROOT, "source", "Nyforge.Core", "Nui", "PropertyDefinitions.cs")

# Optionally verify against the upstream Nyrqis copy (same file in the
# sibling checkout, if present).
NYRQIS_UPSTREAM = os.path.join(
    NYFORGE_ROOT, "..", "Nyrqis", "source", "nyhal-linux-backend",
    "ui", "contracts", "nui-api-v1.json")


# ---------------------------------------------------------------------------
# C# helpers
# ---------------------------------------------------------------------------

def _cs_string_array(items: List[str]) -> str:
    """Return a C# new[] { ... } literal for the given items."""
    if not items:
        return "Array.Empty<string>()"
    inner = ", ".join(f'"{v}"' for v in items)
    return f"new[] {{ {inner} }}"


def _indent(level: int, text: str) -> str:
    """Indent *text* by *level* spaces per line."""
    pad = "    " * level
    return "\n".join(pad + line if line.strip() else "" for line in text.splitlines())


# ---------------------------------------------------------------------------
# ComponentContracts.cs
# ---------------------------------------------------------------------------

COMPONENTS_HEADER = """\
namespace Nyforge.Core.Nui;

/// <summary>
/// A single component's declared API contract: what properties it has,
/// what events it can raise, and (eventually) what Nyrqis actions it can
/// invoke.  Regenerated from the Nyrqis API Registry by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record ComponentContract(
    string Type,
    string Category,
    IReadOnlyList<string> Properties,
    IReadOnlyList<string> Events,
    IReadOnlyList<string>? Actions = null)
{
    /// <summary>Component-instance actions a behavior's DO clause can target on this type. Empty if none declared.</summary>
    public IReadOnlyList<string> Actions { get; init; } = Actions ?? Array.Empty<string>();
}

/// <summary>
/// The NUI component vocabulary — auto-generated from the Nyrqis API
/// Registry (engineering/registry/nui-api-v1.json).  Anything the
/// Component Palette shows in Nyforge.Shell must come from here
/// (NFC-001 §4.3).  To add or change a component, edit the registry
/// and re-run:  python tools/generate_contracts.py
/// </summary>
public static class ComponentContracts
{"""

FOOTER_CONTRACTS = """\
    private static readonly Dictionary<string, ComponentContract> ByType =
        All.ToDictionary(c => c.Type, StringComparer.Ordinal);

    public static bool TryGet(string type, out ComponentContract? contract) =>
        ByType.TryGetValue(type, out contract);

    public static IEnumerable<ComponentContract> ByCategory(string category) =>
        All.Where(c => string.Equals(c.Category, category, StringComparison.Ordinal));
}"""

# Category grouping order (matches the schema). Registry entries are
# grouped under these headers in this order; unknown categories sort
# last in encounter order (the generator only uses this to emit the
# `// Category` comment lines, preserving registry order otherwise).
CATEGORY_ORDER = [
    "Basic", "Layout", "System", "Navigation",
    "Shell", "Data", "Form", "Media", "Developer",
]


def generate_contracts(components: List[Dict[str, Any]]) -> str:
    """Return the full content of ComponentContracts.cs."""
    lines: List[str] = [COMPONENTS_HEADER, ""]
    lines.append('    public static readonly IReadOnlyList<ComponentContract> All = new[]')
    lines.append("    {")

    seen_categories = set()
    for comp in components:
        cat = comp.get("category", "")
        if cat not in seen_categories:
            seen_categories.add(cat)
            lines.append(f"        // {cat}")
        events: List[str] = comp.get("events") or []
        actions: List[str] = comp.get("actions") or []
        raw_props = comp.get("properties") or []
        prop_names = [
            p["name"] if isinstance(p, dict) else str(p) for p in raw_props]
        props_str = _cs_string_array(prop_names)
        events_str = _cs_string_array(events)
        actions_str = _cs_string_array(actions)

        # Build the component line.
        # Signature: (Type, Category, Properties, Events [, Actions: ...])
        parts = [
            f'"{comp["type"]}"',
            f'"{cat}"',
            props_str,
            events_str,
        ]
        if actions:
            parts.append(f"Actions: {actions_str}")
        line = "        new ComponentContract(" + ", ".join(parts) + "),"
        lines.append(line)

    lines.append("    };")
    lines.append("")
    lines.append(FOOTER_CONTRACTS)
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------
# NuiSystemActions.cs
# ---------------------------------------------------------------------------

SYSTEM_ACTIONS_HEADER = """\
namespace Nyforge.Core.Nui;

/// <summary>
/// An action not scoped to a specific component instance — the
/// "Nyrqis.*" calls from the original design doc.  Auto-generated from
/// the Nyrqis API Registry (engineering/registry/nui-api-v1.json) by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record SystemActionContract(
    string Name,
    IReadOnlyList<string> ArgumentNames);

public static class NuiSystemActions
{"""

FOOTER_ACTIONS = """\
    private static readonly Dictionary<string, SystemActionContract> ByName =
        All.ToDictionary(a => a.Name, StringComparer.Ordinal);

    public static bool TryGet(string name, out SystemActionContract? contract) =>
        ByName.TryGetValue(name, out contract);
}"""


def generate_system_actions(actions: List[Dict[str, Any]]) -> str:
    """Return the full content of NuiSystemActions.cs."""
    lines: List[str] = [SYSTEM_ACTIONS_HEADER, ""]
    lines.append('    public static readonly IReadOnlyList<SystemActionContract> All = new[]')
    lines.append("    {")
    for action in actions:
        args_str = _cs_string_array(action.get("arguments") or [])
        lines.append(f'        new SystemActionContract("{action["name"]}", {args_str}),')
    lines.append("    };")
    lines.append("")
    lines.append(FOOTER_ACTIONS)
    return "\n".join(lines) + "\n"# ---------------------------------------------------------------------------
# PropertyDefinitions.cs — typed per-property metadata (NFS-006)
# ---------------------------------------------------------------------------

PROPERTY_DEFS_HEADER = """\
namespace Nyforge.Core.Nui;

/// <summary>
/// Typed metadata for one component property (NFS-006 / the Nyrqis API
/// Registry): what the Inspector renders, validates against, and binds.
/// Auto-generated from engineering/registry/nui-api-v1.json by
/// tools/generate_contracts.py — never edit by hand.
/// </summary>
public sealed record PropertyDefinition(
    string Name,
    string Type,
    object? DefaultValue = null,
    bool Bindable = true,
    bool Required = false,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? EnumValues = null,
    string? Units = null)
{
    /// <summary>Enum choices when <see cref="Type"/> is "enum"; empty otherwise.</summary>
    public IReadOnlyList<string> EnumValues { get; init; } = EnumValues ?? Array.Empty<string>();
}

/// <summary>
/// Per-component property metadata — the typed contract the Inspector
/// builds its editors from (one editor per property, chosen by
/// <see cref="PropertyDefinition.Type"/>). Generated from the Nyrqis
/// API Registry; add or change a property in the registry and re-run
/// tools/generate_contracts.py.
/// </summary>
public static class PropertyDefinitions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<PropertyDefinition>> ByType =
        new Dictionary<string, IReadOnlyList<PropertyDefinition>>
        {
"""

PROPERTY_DEFS_FOOTER = """\
        };

    /// <summary>Metadata for every property of the given component type; empty if unknown.</summary>
    public static IReadOnlyList<PropertyDefinition> For(string type) =>
        ByType.TryGetValue(type, out var defs) ? defs : Array.Empty<PropertyDefinition>();
}"""


def _cs_object_literal(value: Any) -> str:
    """Emit a C# literal for a registry default value."""
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, str):
        return '"' + value.replace('\\', '\\\\').replace('"', '\\"') + '"'
    if isinstance(value, list):
        inner = ", ".join(_cs_object_literal(v) for v in value)
        return f"new object[] {{ {inner} }}"
    return "null"


def generate_property_definitions(components: List[Dict[str, Any]]) -> str:
    """Return the full content of PropertyDefinitions.cs."""
    lines: List[str] = [PROPERTY_DEFS_HEADER]
    for comp in components:
        type_name = comp["type"]
        props = comp.get("properties") or []
        if not props:
            continue
        lines.append(f'            ["{type_name}"] = new[]')
        lines.append("            {")
        for p in props:
            name = p["name"] if isinstance(p, dict) else p
            ptype = p.get("type", "string") if isinstance(p, dict) else "string"
            default = p.get("default") if isinstance(p, dict) else None
            bindable = p.get("bindable", True) if isinstance(p, dict) else True
            required = p.get("required", False) if isinstance(p, dict) else False
            pmin = p.get("min") if isinstance(p, dict) else None
            pmax = p.get("max") if isinstance(p, dict) else None
            enums = p.get("enumValues") if isinstance(p, dict) else None
            units = p.get("units") if isinstance(p, dict) else None

            args = [f'"{name}"', f'"{ptype}"']
            if default is not None:
                args.append(f"DefaultValue: {_cs_object_literal(default)}")
            if bindable is not True:
                args.append("Bindable: false")
            if required:
                args.append("Required: true")
            if pmin is not None:
                args.append(f"Min: {pmin}")
            if pmax is not None:
                args.append(f"Max: {pmax}")
            if enums:
                inner = ", ".join(f'"{e}"' for e in enums)
                args.append(f"EnumValues: new[] {{ {inner} }}")
            if units:
                args.append(f"Units: \"{units}\"")
            lines.append(
                f'                new PropertyDefinition({(", ".join(args))}),')
        lines.append("            },")
    lines.append(PROPERTY_DEFS_FOOTER)
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def load_registry() -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """Load and return (components, system_actions) from the registry."""
    with open(REGISTRY_PATH, "r", encoding="utf-8") as fh:
        registry = json.load(fh)
    components = registry.get("components") or []
    system_actions = registry.get("systemActions") or []

    # Validate: every component must have a type.
    for i, comp in enumerate(components):
        if not comp.get("type"):
            raise ValueError(f"component at index {i} missing 'type'")

    # Validate: every system action must have a name.
    for i, action in enumerate(system_actions):
        if not action.get("name"):
            raise ValueError(f"system action at index {i} missing 'name'")

    return components, system_actions


def check_upstream() -> None:
    """If the Nyrqis sibling checkout is present, verify the vendored
    copy matches upstream (warn but don't fail — upstream is the source
    of truth, vendoring is the convenience copy)."""
    if not os.path.isfile(NYRQIS_UPSTREAM):
        return
    with open(NYRQIS_UPSTREAM, "r", encoding="utf-8") as fh:
        upstream = json.load(fh)
    with open(REGISTRY_PATH, "r", encoding="utf-8") as fh:
        vendored = json.load(fh)
    if vendored != upstream:
        print(
            f"WARNING: vendored registry differs from upstream Nyrqis "
            f"({NYRQIS_UPSTREAM}) — consider syncing.", file=sys.stderr)
    else:
        print("upstream registry OK: vendored copy matches Nyrqis source")


def main() -> None:
    check = "--check" in sys.argv

    components, system_actions = load_registry()
    contracts_cs = generate_contracts(components)
    actions_cs = generate_system_actions(system_actions)
    defs_cs = generate_property_definitions(components)

    outputs = [
        (CONTRACTS_OUT, contracts_cs),
        (SYSTEM_ACTIONS_OUT, actions_cs),
        (PROPERTY_DEFS_OUT, defs_cs),
    ]

    if check:
        changed = False
        for path, content in outputs:
            if os.path.isfile(path):
                with open(path, "r", encoding="utf-8") as fh:
                    if fh.read() != content:
                        print(f"CHANGED: {os.path.relpath(path, NYFORGE_ROOT)}")
                        changed = True
            else:
                print(f"NEW: {os.path.relpath(path, NYFORGE_ROOT)}")
                changed = True
        if changed:
            print("Run without --check to regenerate.", file=sys.stderr)
            sys.exit(1)
        print("all contracts in sync")
        return

    for path, content in outputs:
        with open(path, "w", encoding="utf-8") as fh:
            fh.write(content)
        print(f"wrote {os.path.relpath(path, NYFORGE_ROOT)}")


if __name__ == "__main__":
    main()
