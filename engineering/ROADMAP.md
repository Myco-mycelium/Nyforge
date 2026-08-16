# Roadmap

Phase plan from here, in order. Each phase should land as its own reviewable
change set per NFC-001 §7 (its own NFS if it touches the schema).

## Immediate (before anything else)

- [x] **GitHub Actions CI** (`.github/workflows/build.yml`): restore,
      build, test, then publish a real self-contained `Nyforge.exe` for
      Windows on every push to `main`, and attach it to a GitHub Release
      whenever a `v*` tag is pushed. This is also, finally, what actually
      answers "does this compile" — pushing this repo and watching the
      workflow run is the verification that hasn't happened any other way
      yet.
- [ ] **Verify the build locally too**, once CI has run at least once —
      if CI is green, this is basically done; if CI is red, start here.
      `dotnet restore && dotnet build` from the repo root, fix whatever a
      real compiler finds. Large parts of this repo were authored without
      NuGet access in the environment that wrote them; treat everything as
      "carefully hand-written and manually reviewed, not yet
      compiler-checked," until CI says otherwise.
- [ ] Get `tests/Nyforge.Core.Tests` actually running (`dotnet test` — CI
      does this now, but confirm locally too).
- [ ] Enforce the Core/Shell one-way dependency (NFC-001 §5.2) as an
      automated CI check, not just a convention — e.g. a project-reference
      analyzer step, once the basic build/test pipeline above is confirmed
      green.

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
- [ ] Nested-tree canvas editing (drag a component *into* a Container/Stack/
      Grid, not just onto the root) — the schema already supports nesting
      (`examples/settings-app/settings-app.nstudio` uses it by hand); the
      canvas UI for it is what's missing.
- [ ] Alignment guides, snap-to-grid, multi-select, copy/paste.
- [ ] Component reuse/instancing (`components[]` — currently unused).

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
- [x] **Expression-valued action arguments — partially closed (v0.4).**
      `$state:key` substitution (NFS-005) lets an action reference a
      state's *current value* directly. What's still missing: computing a
      value *from* state (e.g. a boolean Toggle mapping to one of two
      theme name strings) — that needs a real conditional expression, not
      substitution. `examples/settings-app/settings-app.nstudio` still
      uses two static buttons for exactly that reason; see NFS-005 for why
      a full expression language wasn't attempted in the same pass.
- [ ] A real expression language (conditionals at minimum) for action
      arguments, once there's a concrete design for how a node-graph Logic
      Editor would represent it too — see NFM-000 §2.3 on why the visual
      and code paths need to stay trivially equivalent, which is what
      makes rushing this risky.
- [ ] Move NUI schema to `1.0.0` / `Accepted` once the above are solid, per
      NFC-001 §4.1.
- [ ] "Advanced code mode" as an alternate way to author the same behaviors
      — both compile to the same `Behaviors` schema section, per the
      original design doc's "two development approaches, one API" model.

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
- [ ] Responsive breakpoints / multiple screen sizes.
- [ ] Additional palette components not in the v0.1 vocabulary (see
      NUI-SCHEMA.md §4's "not yet implemented" list) — each addition is a
      schema change per NFC-001 §4.3.

## v0.5 — Code generation

- [ ] First NUI → native code exporter (target TBD — the original design
      doc explicitly warns against committing to one implementation
      language too early; this needs its own NFS to pick one deliberately).

## Later, separate effort

- The Nyrqis Desktop Shell prototype (Avalonia-based, Windows-hosted)
  described in the original design document is **out of scope for this
  repository's early milestones** and should live as its own tracked
  effort once Nyforge's project system and schema are stable enough to
  design the Shell's own screens in Forge itself — which is the point of
  building Nyforge first.
