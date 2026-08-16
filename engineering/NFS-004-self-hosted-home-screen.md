---
title: A Self-Hosted Home Screen for Forge
document_id: NFS-004
version: 0.1.0
status: Accepted
classification: Normative
owners:
  - Nyforge Architecture
created: 2026-08-14
updated: 2026-08-14
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
  - NFS-001
---

# NFS-004 — A Self-Hosted Home Screen for Forge

## Problem

NFM-000 §2.5 says re-skinning Forge should not be a special case, but
nothing before v0.4 tested that claim — Forge's own chrome was entirely
hardcoded Avalonia XAML, same as any other desktop app. Per
`engineering/ROADMAP.md`'s v0.4 milestone, this closes that gap for a real,
if modest, piece of Forge's own UI: the Home tab.

## Proposal

1. `HomeViewModel`/`HomePanel` render an `.nstudio` document — chosen via
   `PreferencesService.HomeScreenPath`, falling back to a bundled default
   (`examples/forge-home/forge-home.nstudio`) — as the content of Forge's
   own Home tab, reusing `PreviewElementViewModel` and the same
   type-to-control mapping `PreviewWindow` uses for `▶ Preview`.
2. **A new, deliberately separate command surface**: `ForgeCommands`
   (`source/Nyforge.Shell/Services/ForgeCommands.cs`) — a small, fixed set
   of ids (`cmd_new_project`, `cmd_open_project`, `cmd_save_project`) that,
   when they match a rendered component's `id`, trigger real Forge editor
   commands. This is *not* routed through `Behaviors`/`NuiSystemActions`.

## Why a separate command surface, not reuse of Behaviors

This was the central design question. Reusing `Behaviors` would have been
less code. It was rejected because:

- `NuiSystemActions` (`Nyrqis.Theme.Set`, etc.) describes the API surface
  of a *target Nyrqis app* — the thing Forge helps you build. Forge's own
  "New Project" command is not part of any app's API; it's specific to
  this editor.
- If both used the same `target: "System", name: "..."` shape, a
  `.nstudio` file would become ambiguous: is `Nyrqis.Something` a real
  call a shipped app makes, or a Forge-editor-only command that happens to
  live in a file that *looks* like an app project? That ambiguity directly
  undermines NFC-001 §4.3's anti-drift guarantee, which exists specifically
  so a `.nstudio` file's `Behaviors` section always means the same thing.
- The id-matching approach keeps the distinction structurally obvious:
  `Behaviors`/`Events` continue to mean "logic for the app you're
  designing," full stop, everywhere in the codebase, including in
  `examples/forge-home/forge-home.nstudio` itself (which has an empty
  `behaviors: []` — its buttons do nothing through that mechanism at all).

## Scope, honestly bounded

This is the **Home tab only**. The rest of Forge's chrome (palette, canvas,
inspector, menu bar) is still hardcoded Avalonia — genuinely re-skinning
*all* of Forge is a much larger effort or than one milestone, and this
proposal doesn't claim otherwise. What it does establish: the rendering
path is real and shared with Preview (not a one-off demo), the persistence
is real (survives restarts), and the pattern for extending self-hosting to
more of Forge's chrome later is now concrete rather than aspirational.

## Which Manifest principles this advances

- NFM-000 §2.5 directly — this is its first real test, not just its
  statement.
- NFM-000 §2.4 ("The editor is not the runtime") — reusing
  `PreviewElementViewModel`/the rendering pattern rather than building a
  parallel one keeps "how Forge draws an NUI document" in one place.

## Disposition

**Accepted.** Reflected in `REPOSITORY_STATE.md`, `README.md`, and
`engineering/ROADMAP.md` in the same change set.

---

**End of Document**
