# Nyforge Design System

> The token contract behind Forge's own chrome — and the rules a UI
> designed *in* Forge follows. Documented here so the tool and its output
> converge on one language instead of drifting.

## Principles

1. **Tokens, not literals.** No hardcoded colors, margins, radii, or font
   sizes in Forge's chrome. Every value is a named token; the chrome is a
   composition of tokens.
2. **Themes change appearance, never layout.** Eclipse and Solar define
   the *same keys* (colors + interaction states); spacing, radius, and
   type are theme-independent. Swapping themes therefore re-skins the app
   in place — verified at runtime by `ThemeManager` (see
   `docs/how-to/switching-themes.md`).
3. **Affordance is explicit.** Hover, pressed, selection, focus, and
   disabled are first-class states with their own tokens — interaction is
   never implied by color alone.
4. **Contrast by construction.** Text and interactive surfaces are chosen
   so default text meets **WCAG 2.1 AA** (≥ 4.5:1 normal text). The
   primary button uses the theme's *background* as its text color against
   `AccentStrong`, which is picked per theme to keep that pair AA.

## Token families

Every theme file (`source/Nyforge.Shell/Themes/*.axaml`) defines all of
these; shared structural tokens are duplicated across themes with
identical values so a swap never leaves a missing resource.

### Color (semantic — NUI §6 contract)

`Nyforge.Color.*` / `Nyforge.Brush.*`:

| Token | Meaning |
|---|---|
| `Background` | Window background |
| `Surface` | Panel / control background |
| `SurfaceElevated` | Raised surfaces (cards, palette items) |
| `SurfaceOverlay` | Bars, overlays (status, preview chrome) |
| `TextPrimary` / `TextSecondary` | Primary / secondary text (secondary ≥ 4.5:1 on Surface) |
| `Accent` | Brand / emphasis |
| `Border` / `ControlBorder` | Hairline / control borders |
| `Success` / `Warning` / `Error` | Semantic status |
| `Shadow` | Elevation shadow color |

These exact names are the ones a `.nstudio` project's `themes.overrides`
may target (NUI-SCHEMA.md §6) — Forge's chrome and your app draw from
one set.

### Interaction states (Forge chrome)

| Token | Meaning |
|---|---|
| `Hover` | Pointer-over surface tint |
| `Pressed` | Pointer-down surface |
| `Selection` | Selected item surface |
| `FocusRing` | Keyboard focus indication |
| `TextDisabled` | Disabled text (≥ 3:1 against its surface) |
| `AccentStrong` | Filled (primary) control surface — AA against the theme background |

### Spacing — 4 px grid

`Nyforge.Space.1..6` = **4 / 8 / 12 / 16 / 24 / 32 px**. All chrome
padding, margins, and gaps resolve to this scale. Never use an odd pixel.

### Radius

`Nyforge.Radius.Small` (4) · `Medium` (8) · `Large` (12). Small for
inline controls and list items, medium for cards and buttons.

### Type scale

| Token | Size | Use |
|---|---|---|
| `Nyforge.Type.Caption` | 11 | Section headers, labels, meta |
| `Nyforge.Type.Body` | 13 | Default reading text |
| `Nyforge.Type.Title` | 15 | Group / panel titles |
| `Nyforge.Type.Display` | 20 | Screen-level titles |

## Applying the system

Class-based control styles live in
`source/Nyforge.Shell/Styles/Controls.axaml` (outside `Themes/` so
`ThemeManager` never swaps them; every color is a `DynamicResource`, so
they follow the active theme):

- `section-header` — tracked, muted, uppercase-capable header
  (`TextBlock`)
- `field-label` — muted control label (`TextBlock`)
- `card` — elevated surface + hairline border + medium radius (`Border`)
- `palette-item` — hover affordance + optional `.selected` emphasis
  (`Border`)
- `primary` / `toolbar` — filled vs. flat buttons (`Button`)

Use `Classes="..."` on the control; never restate these values inline.

## Example application

`examples/vault-dashboard/vault-dashboard.nstudio` is designed to this
system: an 8 px layout grid, a three-card stat row with clear
label/value hierarchy, semantic status text, a progress bar, a
`Toggle` bound to document state, and behaviors that demonstrate both a
conditional `IF` and `$state:` argument substitution.

## Cross-platform build

Avalonia is cross-platform, so the one codebase publishes **both**
platform targets from CI (`.github/workflows/build.yml`): `win-x64`
(`Nyforge-win-x64.zip`) and `linux-x64` (`Nyforge-linux-x64.zip`) for
Nyrqis hosts. Self-contained + single-file, so neither needs a runtime
installed.
