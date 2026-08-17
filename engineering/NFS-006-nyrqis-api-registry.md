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
top of them is the v0.6 shell-milestone work.
