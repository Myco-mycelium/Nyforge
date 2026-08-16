---
title: Add the Behaviors Section to the NUI Schema (v0.2.0)
document_id: NFS-002
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

# NFS-002 — Add the Behaviors Section

## Problem

`Components`, `Layouts`, and `Themes` (schema v0.1.0) let you build a static
screen, but nothing on it could *do* anything — every `events` entry was
necessarily `null`. Per `engineering/ROADMAP.md`'s v0.2 milestone, that's the
next real gap: a Logic Editor needs a schema shape to write to before it can
exist.

## Proposal

Add `NuiBehavior` / `NuiCondition` / `NuiAction` (see
`source/Nyforge.Core/Nui/NuiBehavior.cs`) and a document-level `behaviors[]`
array, per `docs/reference/nui-schema/NUI-SCHEMA.md` §7. A component's
`events[eventName]` now holds either `null` or a behavior `id`.

This is a deliberately small model for v0.2: one optional equality
condition, one action, no chaining. See NUI-SCHEMA.md §7 for the full
rationale — the short version is that a bigger model (multi-condition
boolean logic, action chains) would make the visual editor and the
"advanced code mode" harder to keep trivially equivalent, which is a
requirement, not a nice-to-have, per the original design document's
two-modes-one-API principle.

## Schema version impact

**Breaking**, per NFC-001 §4.1: bumps `0.1.0` → `0.2.0`. A `0.1.0`
`.nstudio` file will not open in a build that only understands `0.2.0` (see
`ProjectSerializer.IsCompatible`). This is acceptable per NFC-001 §4.2
because the schema is still `Draft` status — the full backward-compatibility
guarantee only applies once it reaches `1.0.0`.

`examples/settings-app/settings-app.nstudio` was updated in the same change
set to `0.2.0` with real `behaviors[]` entries, replacing the placeholder
comment that previously stood in for this feature.

## Which Manifest principles this advances

- NFM-000 §2.3 ("Two paths to the same API") — action chaining and
  multi-condition logic being explicitly out of scope for v0.2 is what
  keeps a future "advanced code mode" a straightforward 1:1 mapping onto
  the same behaviors, rather than needing its own richer semantics the
  visual graph can't represent.
- NFC-001 §4.3 (anti-drift) — `NuiAction.Name` is validated against either
  `ComponentContracts` (component-instance actions) or the new
  `NuiSystemActions` table (Nyrqis API calls), never a free-form string the
  palette/editor invents.

## Disposition

**Accepted.** Reflected in `REPOSITORY_STATE.md`, `README.md`, and
`engineering/ROADMAP.md` in the same change set.

---

**End of Document**
