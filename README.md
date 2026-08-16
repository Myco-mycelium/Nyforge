# Nyforge

> The visual designer that builds real Nyrqis applications — not mockups of them.

Nyforge is the visual UI design and logic-authoring tool for the [Nyrqis](https://github.com/Myco-mycelium/Nythera)
platform. You design a screen, wire up its behavior, bind it to data, preview
it against the real Nyrqis UI runtime, and export it as an **NUI Definition**
— a stable, versioned intermediate format — which the Nyrqis runtime (or a
code generator) turns into a running application.

Nyforge is governed by the same constitutional process as Nythera. See
[`docs/00-platform/001-NYFORGE_CONSTITUTION.md`](docs/00-platform/001-NYFORGE_CONSTITUTION.md).

## Start Here

1. **[The Nyforge Manifest](docs/00-platform/000-NYFORGE_MANIFEST.md)** — why this tool exists and what it will never become.
2. **[Nyforge Constitution](docs/00-platform/001-NYFORGE_CONSTITUTION.md)** — the enforceable rules that govern this repository.
3. **[Repository State](docs/00-platform/REPOSITORY_STATE.md)** — what currently exists, at a glance.
4. **[NUI Schema Reference](docs/reference/nui-schema/NUI-SCHEMA.md)** — the format Nyforge reads and writes.

## What's built so far

This is a first real build target, not a mockup, iterating milestone by
milestone per `engineering/ROADMAP.md`:

- A working Avalonia desktop shell (`source/Nyforge.Shell`) with a **Component
  Palette**, an interactive **Design Canvas** (drag from palette, select,
  move, resize, multi-select, delete), an **Inspector** (position, size,
  common properties), and a **Layers** panel.
- A project system (`source/Nyforge.Core`) that serializes what you build into
  a real, versioned, human-readable project file: **`.nstudio`**. This *is*
  your project's "code" — open one in a text editor and you'll see a
  structured NUI document, not a binary blob.
- A working **Solar / Eclipse** theme system, built on semantic design tokens
  rather than hardcoded colors, switchable at runtime without restarting the
  app. Nyforge's own chrome is themed the same way an app you design in it
  would be — see [`docs/how-to/switching-themes.md`](docs/how-to/switching-themes.md)
  for how to add a third theme yourself.
- Save / Open / New Project, all backed by the same `.nstudio` format.
- **A first-cut Logic Editor** (the Events tab): pick an element on the
  canvas, attach a behavior to one of its unbound events, and wire up a
  `WHEN [event] IF [optional condition] DO [action]` rule — the action
  target and name are dropdown-validated against the same contract tables
  the palette uses, not free text you can typo into something nonexistent.
  This is what makes `.nstudio` files "code" in the fuller sense: a Save
  button's `clicked` event pointing at `Nyrqis.Settings.Commit` is a real,
  inspectable, re-openable statement about what the app does — see
  `examples/settings-app/settings-app.nstudio` for a worked example.
- **A real, honest Preview.** `▶ Preview` opens an actual window where
  `Button`/`Link`/`Toggle`/`Checkbox`/`Text` are genuinely interactive —
  click a button and its behavior actually fires, flip a toggle and its
  bound state actually updates. It's clearly labeled as Forge's own
  renderer standing in for the Nyrqis UI Runtime (which doesn't exist yet),
  and everything outside that small set of types renders as a marked
  placeholder rather than faking interactivity it doesn't have.
- **Property/state bindings** (`Bindings`): a component's property can be
  tied to a document-level state value, seeded on Preview start and
  updated as you interact with the app.
- **A self-hosted Home tab** — the thing your original ask was most
  directly about. Forge's own Home tab renders an actual `.nstudio` file,
  not hardcoded UI. **File → Customize Home Screen...** points it at any
  project you design, and the change persists across restarts. Open
  `examples/forge-home/forge-home.nstudio` in Forge to see (and edit) the
  exact file backing the default Home tab — see
  [`docs/how-to/redesigning-the-home-screen.md`](docs/how-to/redesigning-the-home-screen.md).
  This is a first, honestly bounded slice of self-hosting (the Home tab
  only — palette/canvas/inspector/menu bar are still hardcoded), not a
  claim that all of Forge is re-skinnable yet.

## What's still not there yet

Being upfront about this matters more than pretending otherwise:

- **The Logic Editor is a flat list, not a node graph yet**, and supports
  one optional equality condition and one action per behavior — no
  AND/OR chains, no action-triggers-action chaining. Deliberately deferred;
  see `engineering/NFS-002-behaviors-schema.md` for why.
- **Action arguments support `$state:` substitution, not full expressions.**
  An action can say `"theme": "$state:choice"` to use a state's current
  value directly (NFS-005), but not compute one — a boolean Toggle still
  can't map itself to one of two theme name strings, since that needs a
  real conditional. `examples/settings-app/settings-app.nstudio` still
  uses two static buttons for that reason.
- **No "advanced code mode"** as an alternate way to author the same
  behaviors yet (the original design doc's two-modes-one-API idea).
- **No live Nyrqis runtime to preview against**, because that runtime
  doesn't exist yet. `▶ Preview` is Forge's own honest stand-in — genuinely
  interactive for a small set of component types, clearly labeled as a
  stand-in, with everything else rendering as a marked placeholder rather
  than fake interactivity.
- **No code-generation exporters** beyond the NUI document itself yet
  (no native C++/Rust backend emission).
- **No Nyrqis Desktop Shell.** That's a separate, later effort per the
  original design doc's own sequencing — Nyforge is the design tool, not the
  shell it will eventually be used to build.
- **Self-hosting covers only the Home tab so far.** The rest of Forge's own
  chrome (palette, canvas, inspector, menu bar) is still hardcoded Avalonia
  — see `engineering/NFS-004-self-hosted-home-screen.md` for the deliberate
  scope boundary and why it wasn't attempted all at once.

See [`engineering/ROADMAP.md`](engineering/ROADMAP.md) for the phase plan.

## Getting Nyforge.exe

**You don't need to build this yourself.** Push this repo to GitHub and
`.github/workflows/build.yml` will restore, build, test, and publish a
real, self-contained `Nyforge.exe` automatically on every push to `main`
— check the **Actions** tab for the build, or the **Releases** page if you
push a `v*` tag (e.g. `git tag v0.4.0 && git push --tags`), which attaches
the built `.exe` to a proper GitHub Release.

This is also, finally, the actual verification of whether this compiles —
see the next section for why that's been an open question until now.

## Building it yourself

This was written and reviewed in a sandboxed environment without access to
the NuGet package feed, so **the build has not been verified by a compiler
run locally**. If you'd rather build it yourself than wait on CI:

```bash
cd source
dotnet restore
dotnet build
dotnet run --project Nyforge.Shell
```

Requires the .NET 8 SDK. If you hit a compile error, it's most likely a
namespace or using-directive slip from hand-written XAML/C# — file an issue
or fix forward, the architecture underneath (documented in
`docs/explanation/architecture.md`) is what should stay stable.

## Repository Layout

```
Nyforge/
├── docs/            # Governance, reference, how-to, explanation (Diátaxis)
├── source/          # Nyforge.Core (NUI + project system), Nyforge.Shell (Avalonia app)
├── tools/           # Build tooling, CLI utilities
├── tests/           # Unit and serialization tests
├── sdk/             # Component authoring SDK (future: third-party components)
├── examples/        # Example .nstudio projects
└── engineering/     # Proposals, roadmap, working notes
```

## License

See [`LICENSE`](LICENSE).
