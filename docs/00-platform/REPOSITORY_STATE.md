---
title: Repository State
document_id: NF-STATE
version: 0.6.0
status: Accepted
classification: Informative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-17
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
---

# Repository State

## Current Milestone: v0.6 — Feature-status enforcement + build verification

Landed 2026-08-17, from the architecture review's **Priority 0** (make the
current code unquestionably real) plus the two doc-consistency items it
surfaced:

- **Build verification resolved.** The "carefully hand-written, not yet
  compiler-checked" status is retired: CI has restored/built/tested
  `Nyforge.sln` on both platforms on every push since the v0.1.0 milestone
  (releases v0.1.0 and v0.2.0 carry the built binaries), and the codebase
  was additionally built and tested **locally** on 2026-08-17 with the
  .NET 8 SDK. README's "Building it yourself" section is now the verified
  recipe; the roadmap's "Verify the build locally too" item is checked.
- **Machine-readable feature status.** `engineering/FEATURE_STATUS.json`
  is now the single source of truth for what's implemented;
  `tools/check_feature_status.py` validates `README.md` and
  `engineering/ROADMAP.md` against it and runs in CI, so documentation
  can't drift from implementation again.
- **Multi-select claim corrected.** The README claimed canvas multi-select;
  the canvas keeps a single `_selectedElement`. README now states it's not
  implemented, and the feature is tracked in FEATURE_STATUS.json.
- **Priority 1, first slice: nested-tree canvas editing.** The canvas now
  edits the real component tree, not just the screen root's children:
  drag a component *into* a container (reparenting preserves its absolute
  position), double-click-add goes into the selected container, delete
  works from anywhere in the tree, the Layers panel shows the hierarchy
  (TreeView), and Preview + the Home tab render nested trees. New
  `Nyforge.Core.Nui.ComponentTree` — pure, position-preserving tree
  operations (find/remove/insert/reparent/absolute-position) — covered by
  12 new unit tests (suite 18 → 30). Follow-ons tracked in
  FEATURE_STATUS.json (`DragReorderWithinParent` and friends).
- **Priority 1, second slice: command-based undo/redo.** New
  `Nyforge.Core.Editing` — `IEditorCommand`, `CommandHistory` (bounded
  undo/redo stacks, redo invalidated on new commands), and the document
  commands (add/delete/move/resize/change-property/reparent/add-behavior/
  delete-behavior). Every edit now flows through the history: a drag
  commits one Move (or Reparent) command on release — never per-move,
  never snapshots. Delete's command also removes behaviors only the
  deleted subtree referenced and restores them on undo. `Ctrl+Z`/
  `Ctrl+Y` and the Edit menu. 11 new unit tests (suite 30 → 41).
- **Priority 1, third slice: multi-select + snap-to-grid.** The canvas
  now supports real multi-selection — Ctrl/Cmd-click toggles membership,
  dragging moves every selected element as one gesture committed as a
  single composite undo command (children of a selected container ride
  along via `TopmostSelected`), and delete removes all selected in one
  undoable step. Drags and resizes snap to the design system's 4 px grid
  (the 4 px grid also gives pointer clicks a natural dead-zone, so a
  click on a multi-selection collapses to that element without
  accidentally moving anything). `CompositeCommand` — 3 new unit tests
  (suite 41 → 44).
- **Priority 1, fourth slice: copy/paste.** `Ctrl+C`/`Ctrl+V` through the
  OS clipboard: `Nyforge.Core.Project.ComponentClipboard` serializes
  copied subtrees (same JSON options as .nstudio so values round-trip as
  native types) and `CloneWithFreshIds` gives every pasted node a fresh,
  document-unique id and strips event wiring — behaviors are
  document-scoped and never cross a clipboard. Paste lands in the
  selected container (or the root) cascading 8 px per paste, as one
  undoable command (composite for multi-copy); the pasted elements are
  selected. Clipboard IO lives in MainWindow's code-behind; the ViewModel
  stays pure. 5 new unit tests (suite 44 → 49).
- **Priority 1, fifth slice: drag-to-reorder + keyboard precision.**
  Dropping a component onto a sibling reorders it immediately before that
  sibling (z-order) as one undoable command — `ReorderComponentCommand`
  restores the exact original index on undo (5 new unit tests, suite
  49 → 54). Arrow keys nudge the selection (4 px grid step, Shift = 20
  px) with one command per press; Shift while resizing locks the aspect
  ratio. With this, every item in the review's Priority 1 list is
  implemented except alignment guides (which need their own design).
- **Roadmap reconciled and extended.** Undo/redo (command-based) is now an
  explicit v0.2 item; the Nyrqis API Registry and the Nyrqis Desktop Shell
  are elevated to a first-class **v0.6** section (the shell is no longer a
  "later, separate effort"); `NFS-006` proposes the registry.

### What exists (cumulative)

- `engineering/FEATURE_STATUS.json` + `tools/check_feature_status.py`
  (CI-enforced doc-consistency).
- `engineering/NFS-006-nyrqis-api-registry.md` — Proposed.
- Everything in the v0.5 milestone below.

## Previous Milestone: v0.5 — Design System + Cross-platform build

A design system for Forge's own chrome (docs/reference/design-system.md):
both themes now define the full token contract — NUI §6 colors,
interaction states (Hover/Pressed/Selection/FocusRing/ControlBorder/
TextDisabled/AccentStrong), a 4 px spacing grid, corner radii, and a type
scale — and the chrome (palette, canvas, inspector, layers, home,
behaviors, preview, status bar) draws from it through shared class-based
control styles (`source/Nyforge.Shell/Styles/Controls.axaml`) instead of
hardcoded margins/fonts/radii. Best-practice affordances: explicit hover
and selection states, AA-contrast primary button, keyboard focus color.

**Cross-platform:** Avalonia publishes both platform targets from the
same codebase — `win-x64` (`Nyforge-win-x64.zip`) and **`linux-x64`
(`Nyforge-linux-x64.zip`) for Nyrqis hosts** — self-contained and
single-file, attached to tagged Releases (`.github/workflows/build.yml`).

**New example:** `examples/vault-dashboard/vault-dashboard.nstudio` — a
Vault Monitor dashboard designed to the system: 8 px layout grid,
label/value card hierarchy, semantic status, a bound Toggle, and
behaviors exercising both a conditional `IF` and `$state:` substitution.

**New example:** `examples/nyrqis-shell/nyrqis-shell.nstudio` — the first
draft of the Nyrqis shell UI itself (the final product the design work
feeds): a 1440×900 workspace on the 4 px grid — StatusBar, NavigationRail,
Sidebar, Toolbar, stat cards, event log, quick actions, Eclipse/Solar
theme switching, a Do-not-disturb Toggle bound to state, a conditional
behavior, and `$state:` substitution — all components/events/actions
within the NUI v0.1 vocabulary (§4) and the contract tables.

**New example:** `examples/nyrqis-shell/security-center.nstudio` — the
second Nyrqis workspace: a 1440×900 Security Center on the same 4 px
grid — posture status bar with a lockdown Toggle bound to state, stat
cards (containers / capabilities / updates), a threat event log, quick
actions, and a conditional lockdown behavior — 71 components, 4
behaviors, 1 binding, all within the contract tables.

**New example:** `examples/nyrqis-shell/vault-workspace.nstudio` — the
third Nyrqis workspace: a 1440×900 Vault screen on the same 4 px grid —
volume stat cards with a storage-usage progress bar, a six-row volume
list with quota indicators, quick actions, and an auto-snapshot Toggle
bound to state with a conditional pause behavior — 71 components, 4
behaviors, 1 binding, all within the contract tables.

### What exists (cumulative)

- The v0.4 milestone below (self-hosted Home + `$state:` substitution).
- The design system + Linux build + vault-dashboard example above.

## Previous Milestone: v0.4 — Self-Hosted Home Screen + $state: Substitution

Two changes landed in this milestone: the self-hosted Home screen (NFS-004)
and, on top of it, `$state:` expression-valued argument substitution
(NFS-005) closing the top item carried over from v0.3's leftovers.

**Status: v0.1–v0.3 (canvas, Logic Editor, Bindings/Preview) plus a first
self-hosting slice, all scaffolded on top of each other; none of it
compiler-verified yet.**

### What exists (cumulative)

- `docs/00-platform/` — Manifest and Constitution.
- `docs/reference/nui-schema/NUI-SCHEMA.md` — NUI schema, **v0.4.0**. The
  Home screen work (NFS-004) didn't touch the schema; **new this
  milestone:** §7.1, `$state:` expression-valued argument substitution
  (NFS-005).
- `docs/how-to/` — saving/loading, theming, and
  `redesigning-the-home-screen.md`.
- `source/Nyforge.Core`:
  - Document model, `Behaviors`, `Bindings`, `BehaviorEvaluator`.
  - **New:** `Nyforge.Core.Runtime.ActionArgumentResolver` — resolves
    `$state:key` argument placeholders against runtime state. Pure and
    framework-free, alongside `BehaviorEvaluator`, per NFC-001 §5.1.
  - **New (from the code-review pass earlier this session, not a separate
    milestone):** `ObjectToInferredTypesConverter` — fixes a real bug where
    `object?`-typed values (`Properties`, `Condition.Value`, `Arguments`,
    `States`) deserialized as `JsonElement` instead of native
    `bool`/`string`/number, which silently broke pattern-matching on
    anything loaded from a file (the theme-switch buttons, toggle seeding).
- `source/Nyforge.Shell`:
  - Everything from v0.1–v0.3 (canvas, Logic Editor, real Preview).
  - The **Home** tab alongside **Design** — renders whatever `.nstudio`
    file `PreferencesService.HomeScreenPath` points at (default: the
    bundled `examples/forge-home/forge-home.nstudio`), reusing the same
    `PreviewElementViewModel` rendering pattern `▶ Preview` uses.
  - `ForgeCommands` — a small, separate id-based command surface for the
    Home screen's buttons (New/Open/Save Project), deliberately *not*
    routed through `Behaviors`/`NuiSystemActions` — see NFS-004 for why
    conflating the two would have broken the schema's anti-drift guarantee.
  - `PreferencesService` — a tiny local JSON prefs file, currently just the
    custom Home screen path, so the choice survives restarts.
  - `File → Customize Home Screen...` — pick any `.nstudio` file, applied
    immediately.
  - **New:** `BehaviorDispatcher` now resolves `$state:` argument
    placeholders before executing, and several previously-silent failure
    paths (unknown theme, missing `windowId`, missing `theme` argument)
    now log an explicit message instead of quietly doing nothing.
- `tests/Nyforge.Core.Tests` — round-trip tests, a regression test for the
  `JsonElement` bug, and new `ActionArgumentResolver` tests.
- `examples/settings-app/settings-app.nstudio` — unchanged this milestone;
  still uses two static buttons rather than one `$state:`-driven toggle,
  because that specific case needs a real conditional expression, which
  `$state:` substitution deliberately doesn't provide (see NFS-005).
- `examples/forge-home/forge-home.nstudio` — the bundled default Home
  screen, and simultaneously the worked example of what a self-hosted
  Forge screen looks like (three buttons with `id`s matching
  `ForgeCommands`, empty `behaviors: []` since none of its interactivity
  goes through that mechanism).
- `engineering/NFS-001` through `NFS-005` — five accepted proposals.
- `engineering/ROADMAP.md` — phase plan for v0.5 onward.

### What does not exist yet

- Self-hosting beyond the Home tab — palette, canvas, inspector, and menu
  bar are still hardcoded Avalonia. NFS-004 is explicit that this was a
  deliberately bounded first slice, not a claim of full self-hosting.
- A real expression language for action arguments — `$state:` (NFS-005) is
  substitution only, not conditionals/computation. A boolean Toggle still
  can't drive a two-way theme choice on its own.
- A node-graph visual representation of behaviors; multi-condition logic;
  action chaining; "advanced code mode."
- A real Nyrqis UI Runtime — `PreviewWindow` remains Forge's own stand-in.
- Code-generation exporters beyond the NUI document itself.
- Nyrqis API Registry integration.
- The Nyrqis Desktop Shell (separate, later effort).
- ~~A compiler-verified build~~ — **resolved 2026-08-17.** CI
  restores/builds/tests/publishes on every push (releases v0.1.0, v0.2.0
  carry the binaries), and the codebase was built and tested locally with
  the .NET 8 SDK the same day. See the v0.6 milestone above.

### Immediate next steps

See `engineering/ROADMAP.md`. Short version: Priority 1 editor-structural
work (nested-tree editing, undo/redo, snapping — tracked in
`engineering/FEATURE_STATUS.json`), then Priority 2 (Nyrqis API Registry,
`engineering/NFS-006`), then Priority 3 (NUI production-grade) and
Priority 4 (the Nyrqis Desktop Shell as a reference application).
