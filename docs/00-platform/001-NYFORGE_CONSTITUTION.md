---
title: Nyforge Project Constitution
document_id: NFC-001
version: 1.0.0
status: Accepted
classification: Normative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-13
ai_assisted: true
review_cycle: Annual
depends_on:
  - NFM-000
  - NPC-001
---

# NFC-001 — Nyforge Project Constitution

## 1. Status of This Document

This document is **normative**. **MUST**, **MUST NOT**, **SHOULD**,
**SHOULD NOT**, and **MAY** follow RFC 2119. Where this document and NFM-000
(The Nyforge Manifest) appear to conflict, the Manifest's philosophy governs
and this document **MUST** be revised.

## 2. Relationship to Nythera's Constitution

Nyforge inherits the document classes, lifecycle, and change process defined
in Nythera's `NPC-001-PROJECT_CONSTITUTION.md`, scoped to this repository.
Where Nythera's Constitution is silent on a Nyforge-specific concern, this
document is authoritative; where the two overlap, Nythera's Constitution
governs, since Nyforge is downstream of the platform it targets.

## 3. Document Classes (this repository)

| Class                     | Prefix | Purpose                                             | Normative? |
| -------------------------- | ------ | --------------------------------------------------- | ---------- |
| Manifest                   | NFM    | Timeless philosophy for Nyforge                     | No         |
| Constitution                | NFC    | Enforceable governance rules for this repository    | Yes        |
| Proposal / Specification   | NFS    | Technical specifications (e.g. NUI schema changes)  | Yes        |
| Architecture Decision Record | ADR  | Records of a specific decision and its rationale     | Yes (historical) |

All normative documents **MUST** carry the same YAML front-matter fields
required by Nythera's NPC-001 §4.

## 4. The NUI Schema Is a Public Contract

4.1. The NUI Schema (`docs/reference/nui-schema/NUI-SCHEMA.md`) **MUST** be
versioned independently of the Nyforge application version, using semantic
versioning per Nythera NPC-001 §7.

4.2. A breaking change to the NUI Schema **MUST NOT** ship without a MAJOR
version increment and a migration note, because `.nstudio` files are user
data and must remain readable across compatible versions (Nythera NPC-001
§10.1 applies here by extension: user data must remain exportable and
readable without vendor lock-in).

4.3. Any component the Component Palette exposes **MUST** correspond to an
entry in the NUI Schema's component vocabulary. Forge **MUST NOT** offer a
palette item, property, or action that has no corresponding schema
representation — this is the concrete enforcement of NFM-000 §3's "silent
divergence" prohibition.

## 5. Editor / Runtime Separation

5.1. `source/Nyforge.Core` (the NUI document model and project system)
**MUST NOT** reference Avalonia or any other UI-framework type. It is the
part of Nyforge that a future Nyrqis UI Runtime could plausibly reuse.

5.2. `source/Nyforge.Shell` (the Avalonia-based editor UI) **MAY** depend on
`Nyforge.Core` but **MUST NOT** be depended upon by it. This is a one-way
dependency, checked informally today and enforceable by CI once one exists
(see `engineering/ROADMAP.md`).

## 6. Self-Hosted Theming

6.1. Nyforge's own editor chrome **MUST** be styled through the same
semantic theme-token system (`docs/how-to/switching-themes.md`) available to
projects built in Forge. A hardcoded, non-token color or style in
`Nyforge.Shell` **SHOULD** be treated as a defect, not a style choice.

6.2. Adding a new theme **MUST NOT** require code changes to component
logic — only a new token resource dictionary.

## 7. Change Process

Same as Nythera NPC-001 §6, scoped to this repository: a contributor opens an
NFS or ADR, states which Manifest principle it advances, and the change is
reflected in `REPOSITORY_STATE.md` and this repo's roadmap in the same
change set. Nyforge does not yet have a multi-contributor Architecture Group;
until one exists, the repository owner fills that role, and this section
**MUST** be revisited once external contributors are onboarded.

## 8. Versioning

Same MAJOR.MINOR.PATCH discipline as Nythera NPC-001 §7, applied separately
to: (a) the Nyforge application itself, and (b) the NUI Schema (§4.1 above).
These two version numbers **MUST NOT** be assumed to move together.

---

## Revision History

| Version | Date       | Change            |
| ------- | ---------- | ------------------ |
| 1.0.0   | 2026-08-13 | Initial constitution, scoped from Nythera NPC-001 |

---

**End of Document**
