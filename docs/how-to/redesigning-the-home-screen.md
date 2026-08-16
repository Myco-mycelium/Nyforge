# How To: Redesign Forge's Home Screen

The **Home** tab (next to **Design**) is not hardcoded Avalonia XAML. It's
whatever `.nstudio` file `File → Customize Home Screen...` currently points
at, rendered live — the same renderer that draws the `▶ Preview` window.

## Try it

1. Open `examples/forge-home/forge-home.nstudio` in Forge (**File → Open...**)
   like any other project. You'll see the exact same three buttons and two
   text elements that make up the default Home tab.
2. Move things around, change the text, add a new Button. Save it — as a
   new file, if you don't want to touch the bundled default (**File → Save
   As...**).
3. **File → Customize Home Screen...** and pick the file you just saved.
   The Home tab updates immediately, and the choice persists across
   restarts (stored in a small local preferences file — see
   `PreferencesService` in `source/Nyforge.Shell/Services/`).

## Why buttons on the Home screen don't use Events/Behaviors

Everywhere else in Forge, a Button's `clicked` event points at a
`Behaviors` entry, which can call a `Nyrqis.*` system action or another
component's declared action (see NUI-SCHEMA.md §7). Those actions describe
the **app you're designing** — the eventual target Nyrqis process's API
surface.

The Home screen's buttons need to do something different: trigger *Forge's
own* editor commands (open the New Project flow, show the Open file
dialog, and so on). Those aren't part of any app's Nyrqis API — they're
specific to this editor. Wiring them through the same `Behaviors` schema
would make a `.nstudio` file ambiguous about whether `Nyrqis.Something` is
a real app-level API call or a Forge-internal command, which breaks the
anti-drift guarantee the rest of the schema depends on (NFC-001 §4.3).

So instead: a small, fixed set of recognized ids
(`ForgeCommands.NewProject`, `.OpenProject`, `.SaveProject` — see
`source/Nyforge.Shell/Services/ForgeCommands.cs`) are checked directly
against a rendered component's `id`. If a Button's `id` in your `.nstudio`
file is `cmd_new_project`, clicking it in the Home tab triggers Forge's New
Project command. Any other id just does nothing — inert, not a guess.

This means you can rename, restyle, and reposition those buttons freely
(text, color, layout are all yours to redesign), but the three ids
themselves are the fixed vocabulary of "things the Home screen can
actually trigger" in v0.4. See `engineering/NFS-004-self-hosted-home-screen.md`
for the full rationale, and `engineering/ROADMAP.md` for where this goes
next (more of Forge's own chrome becoming self-hosted this way).
