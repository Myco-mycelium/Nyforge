# How To: Save and Load a Project

Nyforge projects are `.nstudio` files: a JSON-serialized NUI document (see
`docs/reference/nui-schema/NUI-SCHEMA.md`).

## Saving

- **File → Save** (or `Ctrl+S`) writes the current canvas state to the
  project's `.nstudio` path.
- **File → Save As...** lets you pick a new path; the in-memory project's
  path updates to the new location.
- A new, never-saved project prompts for a path on first save.

Under the hood, `ProjectService.Save` calls
`ProjectSerializer.Serialize(NuiDocument)`, which:

1. Stamps `version` with the NUI schema version Nyforge was built against.
2. Walks the canvas's component tree into `Component` nodes.
3. Writes indented JSON so the file is meaningfully diffable in git.

## Loading

- **File → Open...** reads a `.nstudio` file via
  `ProjectSerializer.Deserialize`, rebuilds the canvas's element tree from
  the `Component` nodes, and restores the active theme from the document's
  `themes` section (or falls back to Eclipse if none is set).
- If the file's `version` is newer than what this build of Nyforge
  understands, you'll get a warning rather than a silent, wrong load.

## Treating a project as source code

Because `.nstudio` is just JSON, you can:

- Put it under git alongside the rest of your Nyrqis project and get
  meaningful diffs on every change.
- Hand-edit it in a text editor for small tweaks without opening Forge.
- Generate or transform it with a script — nothing about the format requires
  Forge to be the one writing it.

## Worked example

`examples/settings-app/settings-app.nstudio` is the Nyrqis Settings app
example from the original design document — a Window with a Sidebar, an
Appearance page containing a Toggle bound to `Nyrqis.Theme.Set`, and a Save
button. Open it with **File → Open...** to see a realistic project rather
than an empty canvas.
