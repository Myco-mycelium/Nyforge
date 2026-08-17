---
title: NUI Schema Reference
document_id: NFS-001
version: 0.4.0
status: Draft
classification: Normative
owners:
  - Nyforge Architecture
created: 2026-08-13
updated: 2026-08-14
ai_assisted: true
review_cycle: PerRelease
depends_on:
  - NFC-001
---

# NFS-001 — NUI Schema Reference

## 1. Purpose

The NUI (Nyrqis UI Definition) Schema is the stable intermediate
representation that sits between Nyforge (the visual editor) and any
consumer of the design — today, Nyforge's own stand-in renderer; eventually,
the real Nyrqis UI Runtime; potentially, native code generators. Nyforge
edits NUI. Nothing downstream should need to know Nyforge exists.

Status is **Draft**. `Components`, `Layouts`, `Themes`, `Behaviors`
(`0.2.0`), and, as of `0.3.0`, `Bindings` are all implemented and
considered stable enough to build against. It stays `Draft` because the
logic model is still deliberately small (see §7, §10) — moving to
`Accepted`/`1.0.0` is gated on that maturing, not on any section being
entirely unimplemented anymore.

## 2. Top-Level Structure

```
NUI Document
├── version          # schema version this document was written against
├── project          # name, id, created/updated timestamps
├── resources         # images, fonts, icons referenced by path or embedded
├── themes            # theme token overrides local to this project, if any
├── screens[]          # one or more top-level Window/Screen definitions
│     ├── id
│     ├── root         # a Component tree
│     └── size          # design-time canvas size (responsive breakpoints: v0.2)
├── components[]       # reusable component definitions (v0.2: currently single-use trees only)
├── states             # named state variables and their initial values (v0.2)
├── behaviors[]        # WHEN(event)/IF(condition)/DO(action) rules — v0.2, see §7
└── bindings[]         # property <-> state bindings — v0.3, see §8
```

## 3. Component Node

Every element on the canvas — a Button, a Container, a Window — is a
Component node:

```json
{
  "id": "btn_save",
  "type": "Button",
  "properties": {
    "text": "Save",
    "enabled": true,
    "visible": true
  },
  "layout": {
    "x": 24, "y": 320, "width": 120, "height": 36
  },
  "events": {
    "clicked": null
  },
  "children": []
}
```

- `type` **MUST** match an entry in the component vocabulary (§4). Forge's
  Component Palette **MUST NOT** offer a type absent from this list (NFC-001
  §4.3).
- `layout` may additionally carry **responsive constraints** (§4.1):
  anchors, min/max bounds, and an aspect ratio. All anchors default
  `false`, so a layout without constraints keeps its absolute authored
  coordinates exactly.
- `events` maps an event name to either `null` (unbound) or the `id` of a
  `Behaviors` entry (§7).
- `children` allows nesting (Container, Stack, Grid, Split View, etc. hold
  child Component nodes).

## 4.1 Responsive Layout Constraints

The `layout` object may carry these optional fields. All anchors default
`false` — a layout without constraints keeps its absolute authored
coordinates exactly.

- `anchorLeft` / `anchorRight` / `anchorTop` / `anchorBottom` (boolean).
  `anchorLeft` fixes the left edge at `x`; `anchorRight` fixes the right
  edge at `containerWidth - x` (`x` doubles as the right inset). **Both
  horizontal anchors together** make the width stretch:
  `width = containerWidth - 2*x`. Vertical is the mirror
  (`anchorTop`/`anchorBottom` with `y`).
- `minWidth` / `maxWidth` / `minHeight` / `maxHeight` (non-negative
  integers; `min* <= max*` when both present). Clamp the computed or
  authored size.
- `aspectRatio` (positive number). Derives the non-stretched axis when
  exactly one axis stretches; otherwise the authored size stands (the
  designer chose it).

Resolution rules are implemented identically in Nyforge
(`ResponsiveLayout.Compute`) and the Nyrqis runtime floor
(`resolve_layout`, differential-tested); both import gates validate the
constraint fields. Example — a bottom-docked, full-width taskbar that
stays usable on narrow windows:

```json
"layout": {
  "x": 0, "y": 0, "width": 1440, "height": 80,
  "anchorLeft": true, "anchorRight": true, "anchorBottom": true,
  "minWidth": 1200, "maxWidth": 1600, "maxHeight": 96
}
```

## 4. Component Vocabulary (v0.1)

The set of `type` values Forge's v0.1 palette exposes, grouped as in the
original design doc:

**Basic:** `Text`, `Icon`, `Image`, `Button`, `Link`, `Input`,
`PasswordField`, `Checkbox`, `Radio`, `Toggle`, `Slider`, `ProgressBar`

**Layout:** `Container`, `Stack`, `Grid`, `FlexLayout`, `SplitView`,
`ScrollView`, `Card`, `Panel`, `Toolbar`, `StatusBar`

**System:** `Window`, `Dialog`, `Notification`

**Navigation:** `Sidebar`, `NavigationRail`, `Tabs`, `Breadcrumbs`

Components listed in the original design doc but **not yet implemented** in
v0.1 (`ContextMenu`, `StartMenu`, `Taskbar`, `SystemTray`, `FilePicker`,
`SettingsPanel`, `PermissionPrompt`, `AuthenticationScreen`,
`PageNavigation`, `CommandPalette`, `FileBrowser`, `Terminal`, `CodeEditor`,
`MediaPlayer`, `DataTable`, `Graph`, `Calendar`, `Map`, `WebView`) **MUST
NOT** appear in the palette until they have a corresponding entry here per
NFC-001 §4.3. Adding one is a schema change, versioned per §9.

## 5. Property Contract

Per-component property/event/action sets (the "Nyrqis API contract" from the
original design doc) are declared in
`source/Nyforge.Core/Nui/ComponentContracts.cs` today, as a static table.
This is a placeholder for a future Nyrqis API Registry integration — see
`engineering/ROADMAP.md`. Until that integration exists, this static table
**is** the source of truth, and any addition to it **MUST** be reflected
here.

## 6. Themes

```json
{
  "tokens": {
    "Background": "#0B0D10",
    "Surface": "#15181D",
    "SurfaceElevated": "#1D2128",
    "SurfaceOverlay": "#242832",
    "TextPrimary": "#EDEFF3",
    "TextSecondary": "#9AA1AC",
    "Accent": "#6C8CFF",
    "Border": "#2A2E37",
    "Shadow": "#00000066",
    "Success": "#4CC38A",
    "Warning": "#E5B567",
    "Error": "#E5686B"
  }
}
```

This token set matches the original design doc's theme system exactly.
Solar and Eclipse are the two built-in token sets (see
`docs/how-to/switching-themes.md`); a project may override individual tokens
without redefining the whole theme.

## 7. Behaviors (v0.2)

A `Behaviors` entry is one `WHEN [event] IF [condition] DO [action]` rule:

```json
{
  "id": "behavior_commit_settings",
  "condition": null,
  "action": {
    "target": "System",
    "name": "Nyrqis.Settings.Commit",
    "arguments": {}
  }
}
```

- **WHEN** is implicit: a behavior is only ever reached because some
  component's `events` map pointed at its `id` (§3). A behavior with no
  component referencing it is inert.
- **IF** (`condition`) is optional. When present, it's a single equality
  check against a document-level `states` value:
  ```json
  "condition": { "state": "theme", "operator": "equals", "value": "Eclipse" }
  ```
  `operator` is `equals` or `notEquals` in v0.2. Multi-condition boolean
  logic (AND/OR chains) is v0.3+ scope.
- **DO** (`action`) has a `target`: either `"System"` (a Nyrqis API call —
  see `NuiSystemActions` in `Nyforge.Core`, e.g. `Nyrqis.Theme.Set`) or
  another component's `id` on the same screen (that component's own
  declared `Actions`, e.g. a `Window`'s `Close`). Same anti-drift rule as
  §4.3 and §5 applies: the Logic Editor must not offer a `name` absent
  from the relevant contract table.
- Action chaining (a DO that triggers another WHEN) is explicitly **not**
  supported in v0.2 — each behavior fires exactly one action. This keeps
  the visual graph and the "advanced code mode" trivially equivalent to
  each other, which is the point of the two-modes-one-API model in the
  original design document.

### 7.1. Expression-valued arguments (v0.4)

An `arguments` value that is a string starting with `$state:` is resolved
against the current runtime state before the action executes, instead of
being used as a literal:

```json
{
  "action": {
    "target": "System",
    "name": "Nyrqis.Notification.Show",
    "arguments": { "message": "$state:lastSavedMessage" }
  }
}
```

- Resolution is plain substitution, not an expression language: no
  ternaries, no concatenation, no nested lookups. A `$state:key` reference
  to a state that doesn't exist is left as the literal placeholder text
  (visible in the Preview event log) rather than silently becoming `null`
  — a missing state is an authoring mistake worth surfacing, not papering
  over.
- This does **not** by itself let one Toggle drive a two-way theme choice
  the way the original design sketch implied (see NFS-003) — that would
  need a boolean-to-string mapping (a real expression, e.g. a ternary),
  which is still out of scope. `$state:` substitution covers "pass this
  state's value through unchanged," not "compute something from it." See
  `examples/settings-app/settings-app.nstudio`, which still uses two
  static buttons for that reason.
- Reference implementation: `Nyforge.Core.Runtime.ActionArgumentResolver`
  (pure, framework-free, called by `Nyforge.Shell`'s `BehaviorDispatcher`
  before dispatching — see NFS-005).

## 8. Bindings (v0.3)

A `Bindings` entry ties a component's property to a document-level `states`
value:

```json
{
  "component": "toggle_eclipse",
  "property": "value",
  "state": "useDarkTheme"
}
```

- On load (or Preview start), the component's `property` is **seeded**
  from the current value of `state`.
- When the user interacts with that property at runtime (e.g. flips the
  Toggle), the **state** is updated to match — see
  `PreviewViewModel.OnPropertyInteraction` in `Nyforge.Shell` for the
  reference implementation of this direction.
- **Partially closed gap (v0.4):** a `Behaviors` action's `arguments`
  (§7.1) can now reference a state's *current value* directly via
  `$state:key` substitution. What's still not possible: computing a value
  *from* a state (e.g. mapping a boolean Toggle to one of two theme name
  strings) — that needs a real expression, not substitution. See §7.1 and
  `examples/settings-app/settings-app.nstudio`, which still uses two
  static buttons for exactly this reason.- Multiple components may bind to the same state; multiple bindings on the
  same component/property pair is undefined behavior in v0.3 (last one
  read wins, not guaranteed which).

## 8.1 Localization

A document may carry a `locales` section — the active locale plus
per-locale string tables:

```json
"locales": {
  "active": "en",
  "tables": {
    "en": { "search.label": "Search", "notif.dnd": "Notifications paused until disabled" },
    "af": { "search.label": "Soek", "notif.dnd": "Kennisgewings onderbreek" }
  }
}
```

- Any string value — a component property, a reusable component's
  `overrides`, or a `Behaviors` action `arguments` value — may be a
  `$localize:key` reference. It resolves through the **active** locale's
  table (`Localize.Resolve` in Nyforge.Core; `resolve_text` in the
  Nyrqis floor). Keys are `[A-Za-z0-9_.-]+`.
- A reference whose key is missing from the active table is a **validation
  error** (ER-NUI-019) at design time and a hard import-gate rejection on
  the Nyrqis side — fail-closed, like every other dangling reference. At
  resolution time a missing key stays as the literal placeholder.
- The active locale `MUST` have a table; tables `MUST` map string keys to
  string values.
- `$localize:` is plain substitution, deliberately in the same family as
  `$state:` (§7.1) — no pluralization/formatting rules yet.

## 9. Versioning and Compatibility

Per NFC-001 §4.1–4.2: this schema uses semantic versioning independent of
the Nyforge application version.

- **0.x** (current): breaking changes are expected between minor versions
  while the schema is in `Draft` status. The bump from `0.1.0` to `0.2.0`
  (adding `Behaviors`), `0.2.0` to `0.3.0` (adding `Bindings`), and `0.3.0`
  to `0.4.0` (adding `$state:` argument substitution, §7.1) are each such a
  break — which is exactly what the **migration chain** is for
  (`NuiSchemaMigrations` in `Nyforge.Core`): an older `.nstudio` file is
  moved forward one step at a time to the current schema before parsing
  (0.2.0 → 0.3.0 adds the `bindings` section, 0.3.0 → 0.4.0 adds
  `states`), **in memory only** — the file on disk is untouched until a
  save, and opening reports the chain that ran rather than migrating
  silently. Every `.nstudio` file still records the schema version it was
  written against, so a genuinely future or unknown version fails loudly
  (`NuiVersionMismatchException`) instead of being silently
  misinterpreted.
- Moving to `Accepted`/**1.0.0** is gated on the logic model maturing
  (multi-condition behaviors, action chaining, a real expression language
  beyond plain substitution — see §10 and `engineering/ROADMAP.md`), not on
  any section being unimplemented, since all four sections now have a
  first implementation.

## 10. Non-Goals for v0.4

- No breakpoint-specific **visibility** (show/hide per size band) or
  multi-canvas size authoring — the constraint system (§4.1) adapts one
  design to any container size, but `size` is still a single
  design-time canvas size.
- No visual extraction of reusable instances from a selection (the
  `components[]` masters + `componentRef` instances are real and
  resolved by `ReusableComponentResolver`; the palette/layers UI for
  creating a master from a selection is the follow-on).
- No multi-condition boolean logic or action chaining in `Behaviors` (§7).
- No real expression language for action arguments — `$state:key` (§7.1)
  is plain substitution only, not ternaries, concatenation, or computed
  values. A boolean Toggle still can't drive a two-way string choice (e.g.
  theme name) on its own.
- No computed/derived bindings — a binding is a direct property/state
  mirror, not an expression over other state.

---

**End of Document**
