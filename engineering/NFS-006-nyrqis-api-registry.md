---
title: The Nyrqis API Registry — one machine-readable platform contract (v0.6)
document_id: NFS-006
version: 0.1.0
status: Implemented
classification: Normative
owners:
  - Nyforge Architecture
  - Nyrqis Platform
created: 2026-08-17
updated: 2026-08-17
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
  - NFS-002
---

# NFS-006 — The Nyrqis API Registry

## Problem

The NUI component vocabulary exists today as **three hand-maintained,
independent copies** of the same contract tables:

| Copy | Location | Language |
| --- | --- | --- |
| Nyforge palette/Inspector/Behaviors source | `source/Nyforge.Core/Nui/ComponentContracts.cs`, `NuiSystemActions.cs` | C# |
| Nyrqis NUI import gate (reference floor) | `source/nyhal-linux-backend/ui/nstudio.py` (`COMPONENT_CONTRACTS`, `SYSTEM_ACTIONS`) | Python |
| Nyrqis NUI import gate (compiled) | `source/nyhal-linux-backend/rust/nyui/src/lib.rs` (`CONTRACTS`) | Rust |

Each copy describes itself as a stand-in, and the ownership claims are
**circular**: Nyforge's tables say they are "a static stand-in for the
future Nyrqis API Registry" (`ComponentContracts.cs`), while the Nyrqis
Rust crate says its tables "mirror NyForge's single source of truth"
(`rust/nyui/src/lib.rs`). Neither can be the source of truth: **the
editor must not define the operating system's API**, and the OS cannot
define its own API by mirroring the editor.

Every schema addition is therefore a three-repo-touch change with no
mechanical guarantee the copies agree. The two Nyrqis copies are already
kept in lockstep by a differential conformance test; the Nyforge copy has
no such guarantee at all.

## Proposal

Introduce the **Nyrqis API Registry**: a single, versioned,
machine-readable platform contract owned by the Nyrqis repository and
*consumed* by both Nyforge and the Nyrqis runtime gate. The registry is
the only place a component, property, event, action, or action argument
is defined; every consumer derives its tables from it.

### Registry structure

The registry is a JSON document (stdlib-parseable on every target —
`System.Text.Json` in C#, the `json` module in Python, `serde_json` in
Rust — no new dependencies anywhere), versioned alongside the NUI schema:

```json
{
  "registryVersion": "1.0",
  "nuiSchemaVersion": "0.4.0",
  "components": [
    {
      "type": "Button",
      "category": "Basic",
      "properties": [
        { "name": "text",      "type": "string", "default": "",     "bindable": true },
        { "name": "enabled",   "type": "boolean", "default": true,  "bindable": true },
        { "name": "visible",   "type": "boolean", "default": true,  "bindable": true }
      ],
      "events": [
        { "name": "clicked",  "args": [] },
        { "name": "pressed",  "args": [] },
        { "name": "released", "args": [] }
      ],
      "actions": [
        { "name": "Close", "target": "Window" }
      ]
    }
  ],
  "systemActions": [
    { "name": "Nyrqis.Theme.Set", "arguments": [{ "name": "theme", "type": "string", "required": true }] },
    { "name": "Nyrqis.Notification.Show", "arguments": [{ "name": "title", "type": "string" }] }
  ]
}
```

Each property/argument entry carries the machine-readable metadata the
2026-08-17 architecture review called for (`type`, `default`, `bindable`,
`required`, and later `min`/`max`/`enumValues`/`units`/`editor`/
`description`/`category`) — the same data that will eventually drive a
generated Inspector, so the registry grows into the typed
`PropertyDefinition` model without a second schema.

### Ownership and consumption

- **Nyrqis owns the registry.** It lives in the Nyrqis repo (e.g.
  `source/nyhal-linux-backend/ui/contracts/nui-api-v1.json`), reviewed
  under the platform's existing governance (ADRs). Adding a component or
  action to Nyrqis is a Nyrqis change first.
- **Nyforge consumes it.** `ComponentContracts.cs`/`NuiSystemActions.cs`
  become *generated* bindings: a small codegen step reads the registry
  and emits the C# records (checked in, so the build needs no network),
  and a conformance test fails if the checked-in generated output is
  stale or drifts from the registry.
- **The Nyrqis gate consumes it.** The Python floor's
  `COMPONENT_CONTRACTS` and the Rust crate's `CONTRACTS` load from the
  same file at build/import time instead of declaring their own copies.
  The existing floor↔crate differential test stays, and a new
  registry↔tables differential makes "mirrors NyForge" claims unnecessary.
- **One source of truth, three consumers, zero drift by construction.**
  The anti-drift rule NFC-001 §4.3 already requires the palette to come
  from the contract tables; this proposal just fixes what *those* tables
  are generated from.

### Versioning and migration

The registry version and the NUI schema version move together
(`registryVersion` majors on breaking contract changes, exactly like
schema minors). Nyforge keeps shipping designs at older registry
versions: `.nstudio` files record the schema version they were authored
against, and the import gate validates against the matching registry
version — the same migration chain the roadmap already plans for the
schema (`0.1 → 0.2 → …`, never mutating user files silently).

## Deliberately not in scope (yet)

- **Not a runtime service.** The registry is a static, versioned document
  for this milestone — a live "query the running platform" API can come
  later and is a Nyrqis-runtime concern, not a Nyforge one.
- **Not a full typed property model.** `min`/`max`/`enumValues`/`units`/
  `editor`/`description` metadata fields are reserved in the structure
  but only `type`/`default`/`bindable`/`required` are populated now, to
  keep the first registry a faithful 1:1 of today's tables. The richer
  fields fill in as the Inspector is generated (Priority 3 work).
- **Not the shell components.** The desktop-shell vocabulary
  (`Taskbar`, `StartMenu`, `WindowFrame`, …) is a separate proposal the
  registry will carry once the shell work (v0.6) designs it.

## Sequencing

Per the 2026-08-17 architecture review's priority order, this is
**Priority 2**: it lands after the editor structural work (nested-tree
editing, undo/redo, snapping — Priority 1) and after the current
build-verification/doc-consistency cleanup (Priority 0). The registry
structure above is deliberately shaped so that work can proceed in
parallel: the file format is fixed now, the consumers migrate one at a
time (Nyrqis Python floor → Nyrqis Rust gate → Nyforge generated C#), and
each migration is independently testable.

## Consequence

A platform addition becomes: change the registry → regenerate consumers →
conformance tests prove agreement. The editor stops being the OS's API
definition, and Nyforge's palette/Inspector/Behaviors dropdowns become
automatically complete — the exact property/action surface the platform
actually supports, with nothing hand-maintained to drift.

## Disposition

Proposed and **implemented 2026-08-17** (Priority 2). The registry
(`engineering/registry/nui-api-v1.json`, vendored from the Nyrqis repo's
`ui/contracts/nui-api-v1.json`) is live as the single source of truth:

- Nyforge C# tables are regenerated from it by `tools/generate_contracts.py`,
  enforced by `tools/check_contracts_synced.py` in CI (both platform jobs).
- The Nyrqis Python floor (`ui/nstudio.py`) and Rust crate (`rust/nyui`)
  read/embed the same file; the conformance gate passes unchanged.

The richer per-property metadata is **no longer future work**: the same
day, every property in the registry became a metadata object
(`name`/`type`/`default`/`bindable`/`required`, plus
`min`/`max`/`enumValues`/`units` where meaningful — Slider value 0–100,
Taskbar position enum, MediaPlayer position stays a number),
`PropertyDefinitions.cs` is generated from it, and the Inspector builds
its editors from `PropertyDefinition.Type` (string/boolean/number/enum),
with writes routed through the undoable history.

The desktop-shell vocabulary is **no longer future work**: the same day,
the registry grew to 63 components across five new categories (Shell,
Data, Form, Media, Developer — Taskbar, StartMenu, WindowFrame,
CommandPalette, LockScreen, List, DataTable, TreeView, DatePicker,
FilePicker, Video, MediaPlayer, Terminal, CodeEditor, …), each with a
real semantic contract, and all three consumers regenerated. Nyforge's
palette now offers the shell's components; building the shell screens on
top of them is the v0.6 shell-milestone work. **2026-08-18: doc #15's
desktop-specific primitives are complete** — `AppGrid`, `Clock`, `Dock`,
and `TitleBar` bring the registry to 70 components (24 Shell types,
including the semantic `Dock` surface: `position`/`pinnedApps`/
`runningApps`/`autoHide`/`iconSize`/`magnify`, `appClicked` → `Launch`),
and the reference `desktop.nstudio` exercises all of them through both
Nyrqis gates and Nyforge's serializer.

Reusable component masters are **no longer future work**: the registry's
`components[]` section is real. A document defines a master there
(validated like any component) and places `componentRef` instances with
`overrides` anywhere — instances omit `type` (both Nyrqis import gates
reject one that declares its own). `ReusableComponentResolver`
materializes an instance as the master's clone plus overrides and
instance children, so editing the master updates every instance; the
`desktop.nstudio` shell example builds its taskbar from one
`TaskbarButton` master with two instances, verified by floor + crate.

Design-time validation is **no longer future work**: `NuiValidator`
(Nyforge.Core) runs check-before-Preview, mirroring the import gate's
hard rules as errors (unknown type/property/event, dangling
behavior/binding/componentRef, unknown action, instance-with-type,
override outside a master's contract) plus the design-only warnings and
infos (duplicate ids, overflow, missing image source, unused behavior,
reusable-instance candidate). Every example fixture must validate with
zero errors in CI.

Localization is **no longer future work**: `NuiDocument.Locales`
(active locale + per-locale string tables) resolves `$localize:key`
references in component properties, reusable overrides, and behavior
arguments; missing keys are rejected before Preview (ER-NUI-019),
mirroring the Nyrqis import gate with byte-identical messages.

The managed asset catalog is **no longer future work**:
`NuiDocument.Resources` declares assets (unique ids, kinds, paths,
sha256 content hashes); `$asset:id` references in properties and
overrides are validated against it (ER-NUI-020), `AssetCatalog`
hashes files for deduplication, and the validator flags missing
resource files — mirroring the Nyrqis import gate.

The NUI expression language (NUI-SCHEMA §7.2) is **no longer future
work**: `$expr:` values (properties, overrides, action arguments) and
condition `expression` fields are validated fail-closed before Preview
(ER-NUI-021) and at both Nyrqis import gates with byte-identical
messages. One deterministic semantics across three implementations —
`Nyforge.Core.Nui.NExpr` (design time), the floor's `ui/nexpr.py`, and
`rust/nyui/src/nexpr.rs` (the shipped crate) — so a screen that
validates and evaluates in Nyforge does exactly the same thing on the
runtime side. The node-graph Logic Editor over that semantics is the
follow-on.

Declarative animations (NUI-SCHEMA §8.3) are **no longer future work**:
the registry now carries the `Nyrqis.Animation.Play` system action (its
`animation` argument), `NuiDocument.Animations` holds the document's
animations (target component, property, duration/delay/easing/repeat/
direction), and a behavior referencing the action must name a declared
animation — enforced identically by the validator (ER-NUI-022) and both
Nyrqis import gates. The desktop shell's Start menu fade plays on
toggle. Keyframes are the documented follow-on.

State scopes (NUI-SCHEMA §8.4) are **no longer future work**:
`NuiDocument.StateScopes` carries the five named state tables (global/
screen/component/session/persistent) referenced as dotted
`scope.key` names in expressions, conditions, bindings, and `$expr:`
arguments — `global` being the named form of the flat `states`
section. The runtime evaluates against the flattened view
(`FlattenedStates` mirroring the floor's `resolve_states`), and
validation is fail-closed at Nyforge (ER-NUI-023 and scope-aware
expression/condition/binding checks) and at both Nyrqis import gates
with byte-identical messages. The shell's persistent theme and session
clock are scoped states; scope lifecycle (what actually persists) is
the runtime's follow-on.
