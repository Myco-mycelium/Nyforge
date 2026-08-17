# Nyforge

> The visual designer that builds real Nyrqis applications — not mockups of them.

Nyforge is the visual UI design and logic-authoring tool for the [Nyrqis](https://github.com/Myco-mycelium/Nythera)
platform. You design a screen, wire up its behavior, bind it to data, preview
it against the real Nyrqis UI runtime, and export it as an **NUI Definition**
— a stable, versioned intermediate format — which the Nyrqis runtime (or a
code generator) turns into a running application.

Nyforge is governed by the same constitutional process as Nythera. See
[`docs/00-platform/001-NYFORGE_CONSTITUTION.md`](docs/00-platform/001-NYFORGE_CONSTITUTION.md).

## Start Here

1. **[The Nyforge Manifest](docs/00-platform/000-NYFORGE_MANIFEST.md)** — why this tool exists and what it will never become.
2. **[Nyforge Constitution](docs/00-platform/001-NYFORGE_CONSTITUTION.md)** — the enforceable rules that govern this repository.
3. **[Repository State](docs/00-platform/REPOSITORY_STATE.md)** — what currently exists, at a glance.
4. **[NUI Schema Reference](docs/reference/nui-schema/NUI-SCHEMA.md)** — the format Nyforge reads and writes.
5. **[Feature Status](engineering/FEATURE_STATUS.json)** — the machine-readable source of truth for what's actually
   implemented; `tools/check_feature_status.py` (run in CI) validates README.md and
   `engineering/ROADMAP.md` against it so documentation can't drift from implementation.

## What's built so far

This is a first real build target, not a mockup, iterating milestone by
milestone per `engineering/ROADMAP.md`:

- A working Avalonia desktop shell (`source/Nyforge.Shell`) with a **Component
  Palette**, an interactive **Design Canvas** (drag from palette, select,
  move, resize, delete), a **metadata-driven Inspector** (property rows
  generated from the Nyrqis API Registry's `PropertyDefinition.Type` —
  string/boolean/number/enum editors, edits undoable), and a **Layers**
  panel. Canvas power-editing (alignment) is
  not implemented yet — see
  [`engineering/FEATURE_STATUS.json`](engineering/FEATURE_STATUS.json),
  the machine-readable feature-status source the docs are validated
  against.
- A project system (`source/Nyforge.Core`) that serializes what you build into
  a real, versioned, human-readable project file: **`.nstudio`**. This *is*
  your project's "code" — open one in a text editor and you'll see a
  structured NUI document, not a binary blob.
- A working **Solar / Eclipse** theme system, built on semantic design tokens
  rather than hardcoded colors, switchable at runtime without restarting the
  app. Nyforge's own chrome is themed the same way an app you design in it
  would be — see [`docs/how-to/switching-themes.md`](docs/how-to/switching-themes.md)
  for how to add a third theme yourself.
- Save / Open / New Project, all backed by the same `.nstudio` format.
- **A first-cut Logic Editor** (the Events tab): pick an element on the
  canvas, attach a behavior to one of its unbound events, and wire up a
  `WHEN [event] IF [optional condition] DO [action]` rule — the action
  target and name are dropdown-validated against the same contract tables
  the palette uses, not free text you can typo into something nonexistent.
  This is what makes `.nstudio` files "code" in the fuller sense: a Save
  button's `clicked` event pointing at `Nyrqis.Settings.Commit` is a real,
  inspectable, re-openable statement about what the app does — see
  `examples/settings-app/settings-app.nstudio` for a worked example.
- **A real, honest Preview.** `▶ Preview` opens an actual window where
  `Button`/`Link`/`Toggle`/`Checkbox`/`Text` are genuinely interactive —
  click a button and its behavior actually fires, flip a toggle and its
  bound state actually updates. It's clearly labeled as Forge's own
  renderer standing in for the Nyrqis UI Runtime (which doesn't exist yet),
  and everything outside that small set of types renders as a marked
  placeholder rather than faking interactivity it doesn't have.
- **Managed asset catalog ($asset: references with content hashing)** —
  a project's `resources` section declares assets (unique ids, kinds,
  paths, sha256 content hashes); `$asset:id` references in component
  properties and reusable overrides are validated against it before
  Preview. `AssetCatalog` hashes files for deduplication, and the
  validator flags missing resource files and duplicate content. The
  shell's wallpaper is a declared asset.
- **Localization ($localize: keys with locale tables)** — a project's
  `locales` section (active locale + per-locale string tables) resolves
  `$localize:key` references in component properties, reusable
  overrides, and behavior arguments — one design, many languages,
  without duplicating layouts. Missing keys are rejected before
  Preview (mirroring the Nyrqis import gate).
- **Responsive layout constraints (anchors, min/max, aspect ratio)** —
  one design adapts instead of one canvas per screen size: `NuiLayout`
  carries anchors (both-horizontal = stretch, bottom-anchor = dock),
  min/max bounds, and an aspect ratio, resolved by
  `ResponsiveLayout.Compute` — mirrored by the Nyrqis runtime's own
  `resolve_layout` (differential-tested) and enforced by both import
  gates. The shell's taskbar stretches and docks itself; a
  breakpoint-specific visibility layer is the documented follow-on.
- **Schema migrations** — old `.nstudio` files keep opening: a
  versioned migration chain (`NuiSchemaMigrations`) moves a document
  forward to the current schema before parsing, in memory only — the
  file on disk is untouched until you save, and opening a migrated
  project reports the chain that ran (never silently). A v0.2.0 file
  that used to throw a version-mismatch error now opens cleanly.
- **A check-before-Preview validator** — `NuiValidator` runs every time
  you hit Preview: errors mirror the Nyrqis import gate at design time
  (unknown type/property/event, dangling behavior/binding/reusable ref,
  unknown action, instance-with-type, override outside a master's
  contract) and block the preview window; warnings (duplicate ids, child
  overflow, missing image source, unused behavior) and infos
  (reusable-instance candidates) are surfaced without blocking. CI
  enforces zero errors across every example design.
- **Reusable component masters (components[]) with componentRef instances and overrides**:
  define a component once (e.g. a `TaskbarButton`) and place instances
  anywhere — `ReusableComponentResolver` materializes the instance from
  the master's clone plus overrides and instance children, so changing
  the master updates every instance. `examples/nyrqis-shell/desktop.nstudio`
  builds its taskbar from one master, and both Nyrqis import gates
  (Python floor + Rust crate) validate the refs and overrides.
- **Property/state bindings** (`Bindings`): a component's property can be
  tied to a document-level state value, seeded on Preview start and
  updated as you interact with the app.
- **A self-hosted Home tab** — the thing your original ask was most
  directly about. Forge's own Home tab renders an actual `.nstudio` file,
  not hardcoded UI. **File → Customize Home Screen...** points it at any
  project you design, and the change persists across restarts. Open
  `examples/forge-home/forge-home.nstudio` in Forge to see (and edit) the
  exact file backing the default Home tab — see
  [`docs/how-to/redesigning-the-home-screen.md`](docs/how-to/redesigning-the-home-screen.md).
  This is a first, honestly bounded slice of self-hosting (the Home tab
  only — palette/canvas/inspector/menu bar are still hardcoded), not a
  claim that all of Forge is re-skinnable yet.
- **A design system for Forge's own chrome** — both themes define the
  full token contract (NUI §6 colors + interaction states, a 4 px
  spacing grid, radii, a type scale) and the chrome composes tokens via
  shared class-based styles instead of hardcoded values — explicit
  hover/selection affordances, an AA-contrast primary button, keyboard
  focus color. See [`docs/reference/design-system.md`](docs/reference/design-system.md).
- **Cross-platform builds in tandem** — the same Avalonia codebase
  publishes **Windows** (`Nyforge-win-x64.zip`) **and Linux**
  (`Nyforge-linux-x64.zip`, the Nyrqis-host target), both
  self-contained single-file.
- **Nested-tree canvas editing** — components can be dragged *into*
  containers (reparenting preserves their on-screen position), added
  directly into a selected container, and deleted from anywhere in the
  tree; the **Layers** panel shows the real component hierarchy; Preview
  and the Home tab render nested trees at their absolute positions.
  Backed by `Nyforge.Core.Nui.ComponentTree` (fully unit-tested).
- **Undo/redo** — every edit is a single command (add, delete, move,
  resize, reparent, property, behavior) on a bounded history: a drag
  commits one command on release, never a command per pointer-move, and
  never whole-project snapshots. `Ctrl+Z` / `Ctrl+Y` (Edit menu too).
  Delete also cleans up behaviors the deleted subtree alone referenced,
  and undo restores them. Backed by `Nyforge.Core.Editing` (fully
  unit-tested).
- **Multi-select and snap-to-grid** — Ctrl/Cmd-click toggles membership;
  dragging moves every selected element as one gesture (a single composite
  undo command; children of a selected container ride along), and delete
  removes them all. Drags and resizes snap to the design system's 4 px
  grid, so canvas work stays grid-clean by construction.
- **Copy/paste** — `Ctrl+C` / `Ctrl+V` through the OS clipboard: copied
  subtrees paste with fresh ids and arrive unbound (behaviors are
  document-scoped), into the selected container or the root, cascading
  8 px per paste so repeats stay visible — and each paste is one
  undoable command.
- **Drag-to-reorder, nudging, aspect-locked resize** — drop a component
  onto a sibling to change stacking order (one undoable command); arrow
  keys nudge the selection (4 px, Shift = 20 px); Shift while resizing
  locks the aspect ratio.
- **A worked dashboard example** — [`examples/vault-dashboard/vault-dashboard.nstudio`](examples/vault-dashboard/vault-dashboard.nstudio)
  is a Vault Monitor designed to the design system: card hierarchy,
  bound state, a conditional behavior, and `$state:` substitution.
- **The Nyrqis desktop shell** — [`examples/nyrqis-shell/desktop.nstudio`](examples/nyrqis-shell/desktop.nstudio)
  is the reference shell screen built from the Shell vocabulary: a
  1440×900 desktop (DesktopSurface + DesktopIcons, Taskbar with
  Start/Search/pinned apps/WorkspaceSwitcher/SystemTray, StartMenu,
  CommandPalette, NotificationCenter, QuickSettings with theme
  switching) plus a lock screen (LockScreen) — 30 components, 8
  behaviors, 6 bindings; it opens in Forge and passes the Nyrqis
  import gate (floor + Rust crate).
- **The Nyrqis window system + power UI** — [`examples/nyrqis-shell/windows.nstudio`](examples/nyrqis-shell/windows.nstudio)
  is the second reference shell screen: WindowFrame + WindowControls
  drive component-targeted actions (Minimize/Maximize/Close), stacked
  Vault-behind-Files windows with a toolbar and lists, and a PowerMenu
  with Sleep/Restart/Shutdown — 21 components, 8 behaviors, 1 binding;
  it opens in Forge and passes the Nyrqis import gate.
- **The Nyrqis widgets + OSD + login** — [`examples/nyrqis-shell/widgets.nstudio`](examples/nyrqis-shell/widgets.nstudio)
  is the third reference shell screen: a WidgetHost holding Clock and
  System Monitor cards, a volume OSD with `$state:`-substituted
  message, and a Login form with submit/cancel — 19 components, 5
  behaviors, 2 bindings; it opens in Forge and passes the Nyrqis
  import gate.
- **The Nyrqis shell UI draft** — [`examples/nyrqis-shell/nyrqis-shell.nstudio`](examples/nyrqis-shell/nyrqis-shell.nstudio)
  is the original first draft of the shell: a 1440×900 workspace with
  StatusBar, NavigationRail, Sidebar, Toolbar, stat cards, an event
  log, quick actions, theme switching, a bound Do-not-disturb Toggle,
  and `$state:` substitution.
- **The Security Center screen** — [`examples/nyrqis-shell/security-center.nstudio`](examples/nyrqis-shell/security-center.nstudio)
  is the second Nyrqis workspace: security posture cards, a threat
  event log, quick actions, and a lockdown Toggle bound to state with
  a conditional behavior — designed to the same 4 px grid and
  contract tables.
- **The Vault Workspace screen** — [`examples/nyrqis-shell/vault-workspace.nstudio`](examples/nyrqis-shell/vault-workspace.nstudio)
  is the third Nyrqis workspace: volume stat cards with a storage
  usage bar, a volume list with quota indicators, quick actions, and
  an auto-snapshot Toggle bound to state with a conditional pause
  behavior.

## What's still not there yet

Being upfront about this matters more than pretending otherwise:

- **The Logic Editor is a flat list, not a node graph yet**, and supports
  one optional equality condition and one action per behavior — no
  AND/OR chains, no action-triggers-action chaining. Deliberately deferred;
  see `engineering/NFS-002-behaviors-schema.md` for why.
- **Action arguments support `$state:` substitution, not full expressions.**
  An action can say `"theme": "$state:choice"` to use a state's current
  value directly (NFS-005), but not compute one — a boolean Toggle still
  can't map itself to one of two theme name strings, since that needs a
  real conditional. `examples/settings-app/settings-app.nstudio` still
  uses two static buttons for that reason.
- **No "advanced code mode"** as an alternate way to author the same
  behaviors yet (the original design doc's two-modes-one-API idea).
- **No live Nyrqis runtime to preview against**, because that runtime
  doesn't exist yet. `▶ Preview` is Forge's own honest stand-in — genuinely
  interactive for a small set of component types, clearly labeled as a
  stand-in, with everything else rendering as a marked placeholder rather
  than fake interactivity.
- **No code-generation exporters** beyond the NUI document itself yet
  (no native C++/Rust backend emission).
- **No Nyrqis Desktop Shell.** That's a separate, later effort per the
  original design doc's own sequencing — Nyforge is the design tool, not the
  shell it will eventually be used to build.
- **Self-hosting covers only the Home tab so far.** The rest of Forge's own
  chrome (palette, canvas, inspector, menu bar) is still hardcoded Avalonia
  — see `engineering/NFS-004-self-hosted-home-screen.md` for the deliberate
  scope boundary and why it wasn't attempted all at once.

See [`engineering/ROADMAP.md`](engineering/ROADMAP.md) for the phase plan.

## Getting a build (Windows or Linux)

**You don't need to build this yourself.** Push this repo to GitHub and
`.github/workflows/build.yml` will restore, build, test, and publish
**both** platform targets automatically on every push to `main`: a real,
self-contained `Nyforge.exe` for Windows (`Nyforge-win-x64.zip`) and a
self-contained Linux binary for Nyrqis hosts (`Nyforge-linux-x64.zip`)
— check the **Actions** tab for the builds, or the **Releases** page if
you push a `v*` tag (e.g. `git tag v0.5.0 && git push --tags`), which
attaches both zips to a proper GitHub Release.

CI is the definitive build verification: every push to `main` restores,
builds, and tests `Nyforge.sln` on both Windows and Linux, and a `v*` tag
attaches both zips to a GitHub Release (see `v0.1.0` and `v0.2.0`). CI also
runs the feature-status/doc-consistency check
(`python3 tools/check_feature_status.py`).

## Building it yourself

The codebase is compiler-verified on every push by CI (both platforms), and
was also built and tested locally on 2026-08-17 with the .NET 8 SDK — the
"carefully hand-written, not yet compiler-checked" status is retired.

```bash
dotnet restore Nyforge.sln
dotnet build Nyforge.sln --configuration Release
dotnet test Nyforge.sln --no-build --configuration Release
```

Requires the .NET 8 SDK. If you hit a compile error, it's most likely a
namespace or using-directive slip from hand-written XAML/C# — file an issue
or fix forward, the architecture underneath (documented in
`docs/explanation/architecture.md`) is what should stay stable.

## Repository Layout

```
Nyforge/
├── docs/            # Governance, reference, how-to, explanation (Diátaxis)
├── source/          # Nyforge.Core (NUI + project system), Nyforge.Shell (Avalonia app)
├── tools/           # Build tooling, CLI utilities
├── tests/           # Unit and serialization tests
├── sdk/             # Component authoring SDK (future: third-party components)
├── examples/        # Example .nstudio projects
└── engineering/     # Proposals, roadmap, working notes
```

## License

See [`LICENSE`](LICENSE).
