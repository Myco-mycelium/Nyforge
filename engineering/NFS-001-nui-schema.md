---
title: Introduce the NUI Schema (v0.1.0)
document_id: NFS-001
version: 0.1.0
status: Accepted
classification: Normative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-13
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
---

# NFS-001 — Introduce the NUI Schema

## Problem

Nyforge needs a project file format before it can have a project system.
Without one, the editor and the eventual Nyrqis runtime would either share
no common contract, or Forge would bake its internal state directly into
whatever format was fastest to hack together — the exact "drift" risk
NFM-000 §3 rules out.

## Proposal

Adopt the NUI Schema described in
`docs/reference/nui-schema/NUI-SCHEMA.md` as the format for `.nstudio`
project files, starting at version `0.1.0`, status `Draft` (the
`Behaviors`/`Bindings` sections are unimplemented pending the Logic
Editor).

## Which Manifest principles this advances

- NFM-000 §2.2 ("The project is the artifact") — directly implemented:
  `.nstudio` is the NuiDocument, serialized, nothing more.
- NFM-000 §2.4 ("The editor is not the runtime") — the schema lives in
  `Nyforge.Core`, which has no Avalonia dependency (NFC-001 §5.1).

## Alternatives considered

- **Binary/proprietary save format.** Rejected: fails NFM-000 §2.2 and
  Nythera NPC-001 §10.1's data-ownership principle (user data must remain
  readable and exportable without vendor lock-in) by extension.
- **YAML instead of JSON.** Considered, not rejected outright — JSON was
  chosen for v0.1 because `System.Text.Json` ships in the BCL with no extra
  dependency, keeping `Nyforge.Core` dependency-free per NFC-001 §5.1. A
  future NFS could propose YAML or a dual-format story if human-editability
  becomes a stronger requirement than it is today.

## Disposition

**Accepted.** Reflected in `REPOSITORY_STATE.md` and this repository's
`README.md` in the same change set.

---

**End of Document**
