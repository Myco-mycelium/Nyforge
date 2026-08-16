---
title: Add Bindings to the NUI Schema, Build a Live-ish Preview (v0.3.0)
document_id: NFS-003
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
  - NFS-002
---

# NFS-003 — Bindings + Preview Stand-In

## Problem

Schema `0.2.0` could describe behavior, but `▶ Preview` still just printed
a status message — nothing on the canvas actually *ran*. Per
`engineering/ROADMAP.md`'s v0.3 milestone, the next real gap was making
Preview an honest, working stand-in rather than a placeholder, and giving
`Bindings` (reserved shape since `0.1.0`) a first real implementation to
make that possible.

## Proposal

1. Formalize `NuiBinding` (`source/Nyforge.Core/Nui/NuiBinding.cs`) —
   `component` / `property` / `state`, per NUI-SCHEMA.md §8.
2. Add `BehaviorEvaluator` (`source/Nyforge.Core/Runtime/`) — pure,
   framework-free condition evaluation, kept in `Nyforge.Core` per NFC-001
   §5.1 because a future Nyrqis UI Runtime could reuse it directly.
3. Add `BehaviorDispatcher` (`source/Nyforge.Shell/Services/`) — the
   host-specific half (what `Nyrqis.Theme.Set` *does* today, in Forge's own
   preview). Deliberately **not** in `Nyforge.Core`, for the same reason
   split in the opposite direction: this part will NOT be reusable as-is
   once a real runtime exists.
4. Add a `PreviewWindow` that renders a small set of component types
   (`Button`, `Link`, `Toggle`, `Checkbox`, `Text`) as real, interactive
   Avalonia controls, seeds their properties from `Bindings`, and dispatches
   their events through `BehaviorDispatcher`. Everything else renders as a
   clearly-labeled placeholder — per NFM-000 §2.1, an unlabeled fake is
   worse than an honest gap.

## A limitation surfaced during this work, documented rather than hidden

Building the settings-app example against a real evaluator surfaced a
genuine schema gap: `NuiAction.Arguments` are static JSON literals, so a
single Toggle can't yet drive `Nyrqis.Theme.Set` with its own live value —
there's no way for an action argument to say "whatever this state
currently is." The original placeholder example glossed over this (a
boolean Toggle bound to a string `theme` state, with a static action that
couldn't actually reflect the toggle). Rather than paper over it, this
change:

- Documents the gap explicitly in NUI-SCHEMA.md §8 and §10 (Non-Goals).
- Reworks `examples/settings-app/settings-app.nstudio` to something the
  *current* schema can correctly express: the Toggle displays state
  (bound, read-only in effect), and two separate buttons drive the actual
  theme change with static arguments. This is a real, working example, not
  an aspirational one.
- Leaves expression-valued action arguments as explicit future work in
  `engineering/ROADMAP.md`, rather than rushing a design for it now.

## Which Manifest principles this advances

- NFM-000 §2.1 ("The canvas is truthful") — both in the preview's own
  placeholder-vs-real rendering split, and in fixing the example rather
  than leaving a demo that implied more than the schema can do.
- NFM-000 §2.4 ("The editor is not the runtime") — the
  Core/`BehaviorEvaluator` vs. Shell/`BehaviorDispatcher` split is the
  concrete enforcement of this for behavior execution specifically.

## Schema version impact

**Breaking**, per NFC-001 §4.1: bumps `0.2.0` → `0.3.0` (`bindings[]`
changes from an untyped reserved array to a typed `NuiBinding[]`).
Acceptable under NFC-001 §4.2 while `Draft`.

## Disposition

**Accepted.** Reflected in `REPOSITORY_STATE.md`, `README.md`, and
`engineering/ROADMAP.md` in the same change set.

---

**End of Document**
