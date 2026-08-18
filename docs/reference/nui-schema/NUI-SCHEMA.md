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

## 4. Component Vocabulary

The set of `type` values the palette exposes, grouped as in the original
design doc. **The authoritative, machine-readable list is the Nyrqis API
Registry** (`engineering/registry/nui-api-v1.json`, NFS-006): `Forge`'s
palette, Inspector, and Behaviors contract tables derive from it, and the
Nyrqis import gates (Python floor + Rust crate) validate against the same
file. Adding a component is a registry change, versioned per §9.

**Basic:** `Text`, `Icon`, `Image`, `Button`, `Link`, `Input`,
`PasswordField`, `Checkbox`, `Radio`, `Toggle`, `Slider`, `ProgressBar`

**Layout:** `Container`, `Stack`, `Grid`, `FlexLayout`, `SplitView`,
`ScrollView`, `Card`, `Panel`, `Toolbar`, `StatusBar`

**System:** `Window`, `Dialog`, `Notification`

**Navigation:** `Sidebar`, `NavigationRail`, `Tabs`, `Breadcrumbs`

**Shell:** `DesktopSurface`, `DesktopIcon`, `Taskbar`, `StartMenu`,
`SystemTray`, `NotificationCenter`, `QuickSettings`, `WorkspaceSwitcher`,
`WindowFrame`, `WindowControls`, `ContextMenu`, `CommandPalette`,
`Launcher`, `AppGrid`, `Search`, `PowerMenu`, `LockScreen`, `Login`,
`Application`, `WidgetHost`, `OSD`, `Dock`, `Clock`, `TitleBar` — the
semantic contracts (not generic rectangles): `Taskbar` knows
`position`/`alignment`/`autoHide`/`pinnedApps`/`runningApps`/
`showClock`/`showTray`; `Dock` knows `position`/`autoHide`/`iconSize`/
`magnify`; `AppGrid` lays out `apps` on a `columns` grid;
`Clock` renders a `format`; `TitleBar` + `WindowControls` frame an app
window (`WindowFrame`).

**Data:** `List`, `ListItem`, `DataTable`, `TreeView`, `Menu`, `MenuItem`

**Form:** `Form`, `DatePicker`, `TimePicker`, `FilePicker`, `SettingsPanel`

**Media:** `Video`, `Audio`, `MediaPlayer`

**Developer:** `Terminal`, `CodeEditor`, `LogViewer`

## 5. Property Contract

Per-component property/event/action sets (the "Nyrqis API contract" from the
original design doc) live in the registry (`engineering/registry/nui-api-v1.json`)
as typed `PropertyDefinition`s (name, type, default, enum values, units,
min/max) and are regenerated into `ComponentContracts.cs` /
`PropertyDefinitions.cs` / `NuiSystemActions.cs` by
`tools/generate_contracts.py` (CI-gated by `tools/check_contracts_synced.py`
per NFC-001 §4.3 and NFS-006). Any addition to the vocabulary **MUST** be
made in the registry first, never in the generated tables by hand.

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

### 7.2. The NUI expression language (v0.4)

A whole-string value starting with `$expr:` — in component properties,
reusable-component overrides, and action arguments — is an **expression**
evaluated against the current document state, rather than a literal:

```json
{
  "action": {
    "target": "System",
    "name": "Nyrqis.Notification.Show",
    "arguments": { "title": "$expr:format(state.clockTime, \"{0}\")" }
  }
}
```

A condition may also use an `expression` field instead of the legacy
`state`/`operator`/`value` equality form (when both are present, the
expression wins):

```json
{
  "id": "behavior_dnd_on",
  "condition": { "expression": "state.doNotDisturb == true" },
  "action": { "target": "System", "name": "Nyrqis.Notification.Show", "arguments": {} }
}
```

**The language is deliberately small and deterministic** — designed for
NUI, not a general-purpose scripting language. One semantics, three
implementations that must agree byte-for-byte:

- `Nyforge.Core.Nui.NExpr` (design time, C#)
- `source/nyhal-linux-backend/ui/nexpr.py` (the reference floor, Python)
- `source/nyhal-linux-backend/rust/nyui/src/nexpr.rs` (the shipped crate)

Grammar (lowest to highest precedence): `||`, `&&`, comparisons
(`==` `!=` `<` `<=` `>` `>=`), `+`/`-`, `*`/`/`/`%`, unary `!`/`-`, then
primaries: numbers, `"strings"` (with `\" \\ \n \t \r \0` escapes),
`true`/`false`, `state.<name>` references (bare `state` is the empty
reference), parenthesized groups, and calls.

Functions (arity-checked at every gate): `if(cond, a, b)`,
`min(a, ...)`/`max(a, ...)` (numeric), `contains(haystack, needle)`, and
`format(value, "{0}" | "{0:.Nf}" | "{0:.Ne}", ...)` (Python-style numeric
specs, translated to .NET's `F`/`E` types in the C# mirror).

**Fail-closed at every gate**: an expression that doesn't parse, names an
unknown function, passes the wrong number of arguments, or references an
undeclared state is rejected by Nyforge's validator (`ER-NUI-021`, before
Preview) and by both Nyrqis import gates with byte-identical messages
(differential-tested). At resolution time a missing state reads as an
empty string (the gate already rejected the document, so this only
matters for hand-edited state).

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

## 8.2 Resources

A document may carry a `resources` section — the managed asset catalog:

```json
"resources": {
  "assets": [
    { "id": "wallpaper", "kind": "image", "path": "assets/wallpaper.png",
      "sha256": "3b0a…(64 hex chars)" }
  ]
}
```

- Every asset declares a document-unique `id`, a `kind` from
  image/svg/icon/font/audio/video/material/animation, and a non-empty
  `path` relative to the project directory. `sha256` is optional but
  must be a 64-char hex string; `AssetCatalog` computes it from the
  file so duplicate content can be coalesced (and the validator warns
  on duplicate content).
- Any string property (or reusable override) may reference an asset via
  `$asset:id`; the reference must name a declared resource — a
  validation error (ER-NUI-020) at design time and a hard import-gate
  rejection on the Nyrqis side, like every other dangling reference.
- The validator also warns when a declared resource's file does not
  exist relative to the project directory (WN-NUI-007).

## 8.3 Animations

A document may carry an `animations` section — declarative, timed
property transitions:

```json
"animations": [
  { "id": "start_menu_fade", "target": "start_menu", "property": "opacity",
    "duration": 200, "delay": 0, "easing": "ease-out",
    "repeat": 0, "direction": "forward" }
]
```

- Every animation declares a document-unique `id`, a `target` that must
  name an existing component (may be omitted — the behavior's triggering
  component), and a non-empty `property` to animate (e.g. opacity,
  position, scale, rotation, blur, color).
- Timing: `duration` (ms, default 300), `delay` (ms, default 0), and
  `repeat` (default 0) must be non-negative integers; `easing` is one of
  linear / ease-in / ease-out / ease-in-out / steps (default
  ease-in-out); `direction` is one of forward / reverse / alternate
  (default forward).
- Animations are **triggered by behaviors**, never hardcoded into a
  component: a behavior whose action is the `Nyrqis.Animation.Play`
  system action (added to the Nyrqis API Registry) plays the animation
  named by its `animation` argument — the reference must name a declared
  animation (validation error ER-NUI-022 at design time; hard import-gate
  rejection, byte-identical, on both Nyrqis sides):

```json
{ "id": "behavior_start_toggle",
  "action": { "target": "System", "name": "Nyrqis.Animation.Play",
               "arguments": { "animation": "start_menu_fade" } } }
```

- One semantics, three implementations: `Nyforge.Core.Nui.NuiAnimation`
  (design time), the reference floor's `animations` validation, and the
  Rust crate — differential-tested byte-for-byte. Multi-point
  **keyframes** (a list of time/value stops) and an animation timeline
  editor are the documented follow-on; the section's shape is designed
  to grow them without a breaking change.

## 8.4 State Scopes

A document may carry a `stateScopes` section — named state tables that
scope where a state lives, so real applications (and the desktop shell)
can separate concerns instead of flattening everything into `states`:

```json
"stateScopes": {
  "global":      { "volume": 60 },                  // named form of `states`
  "session":     { "clockTime": "14:32" },          // this run
  "persistent":  { "theme": "Eclipse" }             // survives restart
}
```

- The five scope kinds are `global`, `screen`, `component`, `session`,
  and `persistent`; each maps to an object table of state keys and
  values. Any other scope name is rejected by every gate.
- `global` is the **named form of the flat `states` section**: a bare
  reference (`state.volume`) resolves against `states` first, then
  `global`. References inside a scope are dot-qualified:
  `state.session.clockTime`, `state.persistent.theme`,
  `state.component.<id>.<key>`.
- Scoped references work **everywhere a state reference works** —
  expression conditions (`state.persistent.theme == "Eclipse"`),
  legacy equality conditions, bindings, `$state:` arguments, and
  `$expr:` expressions. The runtime evaluates against the **flattened
  view** (flat keys plus every scope's entries under their dotted
  names; flat keys win on collision) — `FlattenedStates` in Nyforge
  mirrors the floor's `resolve_states`, and both resolve identically.
- Validation is fail-closed on every gate: unknown scope names and
  non-object tables are rejected (ER-NUI-023 at design time; hard
  import-gate rejection, byte-identical, on both Nyrqis sides), and a
  dotted reference to an undeclared scoped key is an unknown-state
  error in expressions, conditions, and bindings.
- Scope **lifecycle** — what persists `persistent` across restarts vs.
  `session` per run, and how `screen`/`component` tables attach — is
  the runtime's concern (the schema declares the data); the follow-on
  is a state-scope lifecycle spec once the real Nyrqis UI Runtime
  exists.

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
- No multi-condition boolean logic or action chaining in `Behaviors` (§7)
  — the expression language (§7.2) covers computed values and
  conditionals inside a single condition or argument, but a behavior is
  still one condition → one action.
- No node-graph Logic Editor UI — the expression language (§7.2) is the
  underlying semantics; the visual graph editor over it is the follow-on.
- No computed/derived bindings — a binding is a direct property/state
  mirror, not an expression over other state.

---

**End of Document**
