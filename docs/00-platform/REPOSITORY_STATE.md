---
title: Repository State
document_id: NF-STATE
version: 0.5.0
status: Accepted
classification: Informative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-16
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
---

# Repository State

## Current Milestone: v0.5 — Design System + Cross-platform build

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
- **A compiler-verified build.** Still the single most important open
  question — but for the first time, there's now an automatic way to
  answer it: `.github/workflows/build.yml` restores, builds, tests, and
  publishes a real self-contained `Nyforge.exe` on every push to `main`
  (and attaches it to a GitHub Release on a `v*` tag). Push this repo and
  the Actions tab will say, definitively, whether it compiles — that
  hasn't been true at any earlier point in this document's history.

### Immediate next steps

See `engineering/ROADMAP.md`. Short version: verify the build (still not
done), then either extend self-hosting further, close the
expression-valued-arguments gap, or start v0.5 (code generation).
