# Why Nyforge Is Split Into Core and Shell

Nyforge is deliberately two projects, not one:

```
Nyforge.Core     — the NUI document model, .nstudio serialization.
                   No UI framework dependency. No Avalonia. No rendering.

Nyforge.Shell    — the Avalonia desktop app: palette, canvas, inspector,
                   layers, themes. Depends on Core. Core does not know
                   Shell exists.
```

This mirrors the architecture in the original design document almost
exactly:

```
Forge Canvas → NUI Definition → Nyrqis UI Runtime → Actual Rendering
```

`Nyforge.Core` *is* the "NUI Definition" box. It's the part of this
repository that a future Nyrqis UI Runtime could plausibly import wholesale,
because it has no opinion about how anything gets drawn on screen — it just
knows what a Window, a Button, and a Toggle *are*, and how to read and write
that as JSON.

`Nyforge.Shell` is the "Forge Canvas" box: today it's an Avalonia app because
that's the fastest way to get a real, testable Windows desktop UI running
before the Nyrqis UI Runtime exists (same reasoning the original design doc
gives for prototyping the Desktop Shell on Avalonia first). If the Nyrqis UI
Runtime becomes viable as a rendering target for the editor itself, that swap
happens inside Shell, and Core doesn't move.

## Why the project file is JSON you can read

`.nstudio` files are `Nyforge.Core`'s `NuiDocument` serialized with
`System.Text.Json`, indented, with stable property ordering. This is a
direct answer to "I also want to be able to save the projects made from this
app also as the appropriate code" — the project's actual, portable
representation is the NUI document itself, not some editor-only save state.
You can diff two `.nstudio` files in git and the diff will mean something.

## Why theming is a token system, not per-component styling

Every color, spacing, and elevation value a component in `Nyforge.Shell`
uses comes from a `ThemeTokens.axaml` resource key (`{DynamicResource
Nyforge.Color.Accent}`, etc.), not a literal value. `Solar.axaml` and
`Eclipse.axaml` each provide a full set of those resource values; switching
themes at runtime is `Application.Current.Styles` swapping which dictionary
is loaded.

This is what makes "redesign this app's UI/UX by making a new UI within it,
then update whenever I want" actually tractable: adding a third theme is
authoring a third token dictionary, not touching component code. The
longer-term version of this (Forge's own chrome expressed as an NUI document
that Forge itself can open and edit) is scoped for a later milestone —
v0.1 proves the token-swapping half of that promise; self-hosting the editor
UI as an NUI document is the other half, tracked in
`engineering/ROADMAP.md`.
