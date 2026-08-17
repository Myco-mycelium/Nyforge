# Roadmap

Phase plan from here, in order. Each phase should land as its own reviewable
change set per NFC-001 §7 (its own NFS if it touches the schema).

## Immediate (before anything else)

- [x] **GitHub Actions CI** (`.github/workflows/build.yml`): restore,
      build, test, then publish a real self-contained `Nyforge.exe` for
      Windows on every push to `main`, and attach it to a GitHub Release
      whenever a `v*` tag is pushed (releases v0.1.0, v0.2.0). CI also
      runs the feature-status/doc-consistency check
      (`python3 tools/check_feature_status.py`) on both platform jobs.
- [x] **Verify the build locally** — done 2026-08-17 with the .NET 8 SDK:
      `dotnet restore` (NuGet access confirmed), `dotnet build` (0 errors,
      0 warnings after fixing one nullable warning in
      `MainWindowViewModel.SetThemeCommand`), `dotnet test` (18/18 pass).
      The "carefully hand-written, not yet compiler-checked" status is
      retired — see REPOSITORY_STATE v0.6.
- [x] Get `tests/Nyforge.Core.Tests` actually running locally — 18/18
      pass on 2026-08-17.
- [ ] Enforce the Core/Shell one-way dependency (NFC-001 §5.2) as an
      automated CI check, not just a convention — e.g. a project-reference
      analyzer step, once the basic build/test pipeline above is confirmed
      green.

## v0.5 — Design System + Cross-platform build (landed with v0.4, 2026-08-16)

- [x] **Design system for Forge's own chrome** — both themes define the
      full token contract (NUI §6 colors, interaction states, 4 px
      spacing grid, radii, type scale) and the chrome composes tokens via
      shared class-based styles instead of hardcoded values; documented
      in `docs/reference/design-system.md`. Industry best practices:
      explicit hover/pressed/selection/focus states, WCAG 2.1 AA
      contrast by construction (primary button text = theme background
      against `AccentStrong`), 4 px spacing grid, capped type scale.
- [x] **Linux build in tandem with Windows** — CI now publishes both
      `win-x64` and `linux-x64` self-contained single-file builds from
      the same Avalonia codebase; the Linux zip is the Nyrqis-host
      target (`Nyforge-linux-x64.zip`), attached to tagged Releases.
- [x] **`examples/vault-dashboard/vault-dashboard.nstudio`** — a Vault
      Monitor dashboard designed to the system, exercising bound state,
      a conditional behavior, and `$state:` substitution.
- [ ] Verify the tokenized chrome compiles clean on both platforms once
      CI runs (the first compiler pass over the v0.5 XAML).

## v0.2 — Logic Editor & Live-ish Preview

- [x] Stabilize the `Behaviors` section of the NUI schema — done (NFS-002,
      schema `0.2.0`). `Bindings` followed in v0.3 below (NFS-003).
- [x] Visual `WHEN / IF / DO` graph editor (Events tab) — first cut done:
      a flat behavior list per screen, one optional equality condition, one
      action, target validated against `ComponentContracts`/
      `NuiSystemActions`. Not a node-graph UI yet — see the v0.3 entry for
      that.
- [x] Static `ComponentContracts` table extended with per-component
      `Actions` (e.g. `Window.Close`), and a new `NuiSystemActions` table
      for Nyrqis-API-level actions (`Nyrqis.Theme.Set`, etc.) — both are
      still hand-maintained placeholders for the eventual Nyrqis API
      Registry, per NFC-001 §4.3.
- [x] **Nested-tree canvas editing** — drag a component *into* a
      Container/Stack/Grid, not just onto the root (v0.6, Priority 1 first
      slice): reparenting preserves absolute position, double-click-add
      goes into the selected container, delete works from anywhere in the
      tree, the Layers panel is a real hierarchy, and Preview + the Home
      tab render nested trees. Backed by `Nyforge.Core.Nui.ComponentTree`
      (12 unit tests). Follow-on: insertion indicators.
- [x] **Multi-select** — Ctrl/Cmd-click toggles membership; dragging
      moves all selected elements as one gesture (a single composite
      undo command; children of a selected container ride along with it);
      delete removes all selected.
- [x] **Snap-to-grid** — drags and resizes snap to the design system's
      4 px grid.
- [ ] Alignment guides.
- [x] **metadata-driven Inspector** — the Inspector's property rows are
      generated from the Nyrqis API Registry's `PropertyDefinition.Type`
      (string → text box, boolean → checkbox, number → numeric up-down
      with min/max/units, enum → combo box, array → read-only); edits
      route through the undoable history like canvas edits. No more
      hardcoded property fields (v0.6).
- [x] **Drag-to-reorder within a parent** — drop a component onto a
      sibling to change stacking order (one undoable command, exact index
      restored on undo). Alongside: arrow keys nudge the selection (4 px,
      Shift = 20 px) and Shift while resizing locks the aspect ratio. 5
      new unit tests (suite 49 → 54).
- [x] **Copy/paste** — `Ctrl+C`/`Ctrl+V` via the OS clipboard
      (`Nyforge.Core.Project.ComponentClipboard`): pasted subtrees get
      fresh ids and arrive unbound (behaviors are document-scoped), go
      into the selected container or the root, cascade 8 px per paste,
      and land as one undoable command (composite for multi-copy). 5 new
      unit tests (suite 44 → 49).
- [x] **Undo/redo** — command-based (v0.6): every edit is one
      `IEditorCommand` (Add/Delete/Move/Resize/Reparent/ChangeProperty/
      AddBehavior/DeleteBehavior) on a bounded history; a drag
      commits a single Move (or Reparent) command on pointer-up, never a
      command per pointer-move, and never full-project snapshots. Delete
      also removes behaviors only the deleted subtree referenced and
      restores them on undo. `Ctrl+Z` / `Ctrl+Y` + Edit menu. Backed by
      `Nyforge.Core.Editing` (11 unit tests).
- [x] Component reuse/instancing (`components[]` — masters with `componentRef` instances + `overrides`, resolved by `ReusableComponentResolver`).

## v0.3 — Bindings & a Live-ish Preview

- [x] Stabilize `Bindings` — done (NFS-003, schema now `0.3.0`): typed
      `NuiBinding`, seeded on Preview start, updated on interaction.
- [x] A live-ish preview stand-in, honestly labeled as such: `PreviewWindow`
      actually renders `Button`/`Link`/`Toggle`/`Checkbox`/`Text`
      interactively, dispatches events through `BehaviorDispatcher`, and
      shows an event log. Everything else on the canvas still renders as a
      clearly-marked placeholder rather than pretending to be interactive.
- [x] Split behavior execution into `BehaviorEvaluator` (Nyforge.Core, pure
      condition logic, reusable by a future runtime) and
      `BehaviorDispatcher` (Nyforge.Shell, host-specific action execution) —
      the concrete enforcement of NFM-000 §2.4 for this feature.
- [ ] Multi-condition boolean logic (AND/OR chains) and action chaining in
      `Behaviors`, once a node-graph UI (rather than the current flat list)
      makes that navigable — see NFS-002 for why this was deliberately
      deferred.
- [x] **Expression-valued arguments — `$state:key` substitution (v0.4,
      NFS-005)** lets an action reference a state's *current value*
      directly (plain substitution, missing keys left as the literal).
- [x] **A real expression language (v0.4, NUI-SCHEMA §7.2)** — the
      deterministic NUI expression language: `state.name` references,
      comparisons, `&&`/`||`/`!`, and `if`/`min`/`max`/`contains`/`format`
      functions. `$expr:` values (properties, overrides, action
      arguments) and condition `expression` fields are validated
      fail-closed (Nyforge ER-NUI-021; both Nyrqis import gates
      byte-identical) and evaluated identically in Nyforge, the
      reference floor (`ui/nexpr.py`), and the Rust crate — the visual
      and code paths share one semantics (NFM-000 §2.3). The node-graph
      Logic Editor UI is still the follow-on; the expression is its
      underlying model.
- [ ] A real Nyrqis UI Runtime — `PreviewWindow` remains Forge's own
      stand-in.
- [x] **Asset system** — `NuiDocument.Resources` carries the managed
      asset catalog (unique ids, kinds, paths, optional sha256 content
      hashes). `$asset:id` references in component properties and
      reusable overrides must name a declared resource (validator
      ER-NUI-020, mirroring the Nyrqis import gate); `AssetCatalog`
      computes content hashes for deduplication (WN-NUI-008) and the
      validator flags missing resource files (WN-NUI-007). The shell's
      wallpaper is a declared asset referenced via `$asset:wallpaper`.
      Asset *import/preview UI* is the follow-on.
- [x] **Localization** — `NuiDocument.Locales` carries an active locale
      plus per-locale string tables; `$localize:key` references in
      component properties, reusable overrides, and behavior arguments
      resolve through the active table (`Localize.Resolve`) and must
      exist in it — the validator rejects missing keys up front
      (ER-NUI-019), mirroring the Nyrqis import gate. The shell
      fixture's search label and DND message are localized (en/af).
- [x] **Schema migrations** — `NuiSchemaMigrations` (Nyforge.Core): a
      versioned migration chain moves old .nstudio files forward to the
      current schema before parsing, in memory only (the file on disk is
      untouched until save; opening reports what migrated, never
      silently). A v0.2.0 document now opens in a v0.4.0 build where it
      previously threw `NuiVersionMismatchException`. Genuinely
      incompatible versions still fail the gate. See FEATURE_STATUS
      `SchemaMigrations`.
- [ ] Move NUI schema to `1.0.0` / `Accepted` once the above are solid, per
      NFC-001 §4.1.
- [ ] "Advanced code mode" as an alternate way to author the same behaviors
      — both compile to the same `Behaviors` schema section, per the
      original design doc's "two development approaches, one API" model.
- [x] **Validation/linting** — `NuiValidator` (Nyforge.Core): check-before-
      Preview. Errors mirror the Nyrqis import gate at design time
      (unknown type/property/event, dangling behavior/binding/ref, unknown
      action, instance-with-type, override outside contract) and block
      Preview; warnings (duplicate/empty ids, child overflow, missing
      image source, unused behavior) and infos (reusable-instance
      candidate) are surfaced without blocking. Runs in CI over every
      example fixture (zero-error gate). See FEATURE_STATUS `NuiValidator`.

## v0.4 — Self-hosted theming, first slice

- [x] Express a real piece of Nyforge's own editor chrome — the **Home
      tab** — as an NUI document Forge can open and edit on itself, closing
      the first real test of NFM-000 §2.5 ("Re-skinning Forge is not a
      special case"). See NFS-004 for the full design and its honestly
      bounded scope (Home tab only, not the whole editor).
- [x] Persisted, updatable-anytime custom Home screen: **File → Customize
      Home Screen...** points the Home tab at any `.nstudio` file, saved to
      a local preferences file (`PreferencesService`), applied immediately
      and again on next launch.
- [ ] Extend self-hosting to more of Forge's chrome (status bar? a
      dashboard/dock layout?) using the same pattern established by
      NFS-004 — deliberately not attempted in one pass.
- [x] **Responsive breakpoints / multiple screen sizes — constraint
      engine landed (v0.7).** `NuiLayout` carries anchors, min/max
      bounds, and an aspect ratio; `ResponsiveLayout.Compute` (and the
      Nyrqis floor's `resolve_layout`, differential-tested) adapts one
      design to any container size — the desktop shell's taskbar
      stretches and docks itself. Still future: breakpoint-specific
      *visibility* (show/hide per size band) and multi-canvas size
      authoring.
- [ ] Additional palette components not in the v0.1 vocabulary (see
      NUI-SCHEMA.md §4's "not yet implemented" list) — each addition is a
      schema change per NFC-001 §4.3.

## v0.5 — Code generation

- [ ] First NUI → native code exporter (target TBD — the original design
      doc explicitly warns against committing to one implementation
      language too early; this needs its own NFS to pick one deliberately).

## v0.6 — Nyrqis API Registry + the Shell as a reference application

Elevated by the 2026-08-17 architecture review: the editor must not be the
source of truth for the operating system API, and the shell is now a
first-class target built with Forge, not a separate later project. Tracked
in `engineering/FEATURE_STATUS.json` like every other feature.

- [x] **Nyrqis API Registry integration** — `ComponentContracts`/`NuiSystemActions`
      are now regenerated from the vendored Nyrqis API Registry
      (`engineering/registry/nui-api-v1.json`) by `tools/generate_contracts.py`;
      `tools/check_contracts_synced.py` enforces drift-freedom in CI.
      See `engineering/NFS-006`. (completed 2026-08-17)
- [ ] **Nyrqis Desktop Shell reference application** — build the shell's
      screens in Forge as a real end-to-end test (three draft workspaces
      already exist under `examples/nyrqis-shell/`), driving the editor's
      structural gaps (nested trees, reparenting, constraints) with real
      requirements.

## Later, separate effort

- The remaining runtime-side work (a real Nyrqis runtime, code-generation
  exporters) — the shell itself is elevated to v0.6 above; what stays out of
  scope here is the runtime that will eventually run shell screens.
