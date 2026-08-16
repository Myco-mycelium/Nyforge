---
title: Expression-Valued Action Arguments via $state: Substitution (v0.4.0)
document_id: NFS-005
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
  - NFS-002
  - NFS-003
---

# NFS-005 — Expression-Valued Action Arguments

## Problem

NFS-003 surfaced and documented a real gap: `NuiAction.Arguments` are
static JSON literals, so nothing could say "use whatever this state
currently holds." The settings-app example worked around it with two
static buttons instead of one dynamic toggle — a real, working example,
but a workaround, not a closed gap. `engineering/ROADMAP.md` carried this
forward as the top v0.3 leftover item.

## Proposal

A string argument value starting with `$state:` is resolved against the
live runtime state before the action executes — `"theme": "$state:choice"`
becomes `"theme": "Eclipse"` if `states["choice"]` is `"Eclipse"` at fire
time. Implemented as `Nyforge.Core.Runtime.ActionArgumentResolver`, called
by `Nyforge.Shell`'s `BehaviorDispatcher` immediately before dispatching.

## Deliberately not a full expression language

This was the key scope decision, consistent with how NFS-002 scoped
`Behaviors` conditions: plain substitution only.

- No ternaries, no string concatenation, no arithmetic, no nested lookups.
- A missing state key leaves the literal `$state:key` text in place rather
  than resolving to `null` — visible in the Preview event log as an
  obvious authoring mistake, not a silent failure.
- **This does not fully close the gap NFS-003 described.** A boolean
  Toggle still can't drive `Nyrqis.Theme.Set` with a computed theme name,
  because "if true then 'Eclipse' else 'Solar'" is a real expression
  (a conditional), not a substitution. `examples/settings-app/settings-app.nstudio`
  is unchanged by this proposal and still uses two static buttons for
  exactly that reason — this is stated explicitly in NUI-SCHEMA.md §7.1
  and §10 rather than left to be discovered as a surprise.

A full expression language is real future work, not attempted here,
because rushing one risks breaking NFM-000 §2.3 ("two paths to the same
API"): a visual Logic Editor needs to be able to represent whatever the
expression language can express, or the two modes stop being trivially
equivalent. That's a bigger design question than this proposal's scope.

## Where this lives, and why

`ActionArgumentResolver` is in `Nyforge.Core.Runtime`, alongside
`BehaviorEvaluator`, not in `Nyforge.Shell`. Resolving `$state:` references
is host-independent — it doesn't know or care that it's running inside
Forge's own Preview stand-in versus a real Nyrqis process — so it belongs
with the other framework-free runtime logic, per NFC-001 §5.1.

`BehaviorDispatcher.Fire` now resolves arguments exactly once, before
either the system-action or component-action execution paths run, so
every branch downstream sees already-resolved values without needing its
own resolution logic.

## Incidental improvement made alongside this

While touching `BehaviorDispatcher` to thread resolved arguments through,
several silent-failure paths were tightened to log instead: an unknown
theme name, a missing `windowId`, and a missing/wrong-typed `theme`
argument now all produce an explicit event-log message rather than quietly
doing nothing. This isn't part of the `$state:` feature itself, but it was
directly adjacent code and the previous silent-no-op behavior was a real
inconsistency with NFM-000 §2.1 ("the canvas is truthful") — worth fixing
in the same change rather than filing separately and leaving it stale.

## Schema version impact

**Breaking**, per NFC-001 §4.1: bumps `0.3.0` → `0.4.0`, since a literal
argument string that happened to start with `$state:` (unlikely in
practice, but possible) would now be reinterpreted as a placeholder rather
than a literal. Acceptable under NFC-001 §4.2 while `Draft`.

## Disposition

**Accepted.** Reflected in `REPOSITORY_STATE.md`, `README.md`, and
`engineering/ROADMAP.md` in the same change set.

---

**End of Document**
