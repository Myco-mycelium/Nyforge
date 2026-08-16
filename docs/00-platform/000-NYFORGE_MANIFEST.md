---
title: The Nyforge Manifest
document_id: NFM-000
version: 1.0.0
status: Accepted
classification: Informative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-13
ai_assisted: true
review_cycle: Annual
depends_on:
  - NTM-000
---

# NFM-000 — The Nyforge Manifest

## 1. Why Nyforge Exists

Nyrqis needs applications before it has developers who write native Nyrqis
code by hand. Nyforge exists to close that gap: a visual tool where designing
a screen, wiring its behavior, and producing a real, loadable artifact are
one continuous workflow — not "export to a mockup" followed by a separate
implementation phase.

Nyforge is a **development tool**, not a runtime, not a shell, and not the
platform itself. It produces the **NUI Definition** — a stable intermediate
representation — which real Nyrqis components consume. This document exists
so that promise doesn't erode as the tool grows.

## 2. Guiding Principles

1. **The canvas is truthful.** What you see in Forge should be what actually
   renders, not an approximation of it. Where Forge cannot yet render the
   real Nyrqis runtime (because it doesn't exist yet), it must say so
   plainly rather than fake fidelity it doesn't have.
2. **The project is the artifact.** A `.nstudio` file is not a save-state for
   Forge's convenience — it is a structured, versioned, human-readable
   document that fully describes the application. You should be able to read
   one without opening Forge at all.
3. **Two paths to the same API.** Visual logic and native code are equally
   first-class ways of driving the same underlying Nyrqis API. Neither is a
   simplified version of the other.
4. **The editor is not the runtime.** Forge's internal design model must
   never become so entangled with its host UI framework (Avalonia, today)
   that the eventual Nyrqis UI Runtime inherits a dependency on it.
5. **Re-skinning Forge is not a special case.** If Nyforge is meant to let a
   user redesign its own UI/UX from within itself, then Forge's own chrome
   must be built on the same theming and component model as the applications
   it produces — not a separately hardcoded shell that merely looks similar.

## 3. What Nyforge Will Never Become

- A tool that generates only one hardcoded target language, bypassing the NUI
  intermediate representation. (See NTM-000 and the original design
  rationale in `Nyforge.txt` for why this matters.)
- A tool that silently diverges from what the Nyrqis API Registry actually
  exposes. If Forge offers an action in the visual editor, that action must
  exist on the backend, or the action must not be offered.
- A tool whose "Preview" lies about fidelity. A stand-in renderer is
  acceptable during early development; an unlabeled one is not.

## 4. Relationship to Nythera

Nyforge is downstream of, and governed by, the same constitutional process as
[Nythera](https://github.com/Myco-mycelium/Nythera) (see NPC-001). Where this
Manifest and the Nythera Manifest (NTM-000) appear to conflict, NTM-000
governs.

---

**End of Document**
