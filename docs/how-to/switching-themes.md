# How To: Switch Themes, or Add Your Own

Nyforge ships with two built-in themes — **Solar** (bright/warm) and
**Eclipse** (dark/immersive) — and the editor's own chrome is styled through
the same token system a project you design in Forge would use.

## Switching at runtime

**View → Theme → Solar / Eclipse**, or the theme toggle in the status bar.
This calls `ThemeManager.SetTheme(themeName)`, which swaps which resource
dictionary (`Themes/Solar.axaml` or `Themes/Eclipse.axaml`) is merged into
`Application.Current.Styles`. No restart required, and it affects both
Forge's own UI and the live canvas preview simultaneously — because they
both consume the same token keys.

## Adding a third theme

1. Copy `source/Nyforge.Shell/Themes/Eclipse.axaml` to, say, `Nova.axaml`.
2. Set a value for every token key listed in
   `docs/reference/nui-schema/NUI-SCHEMA.md` §6 (`Nyforge.Color.Background`,
   `Nyforge.Color.Surface`, etc. — the `.axaml` file uses the same names
   prefixed with `Nyforge.Color.`).
3. Register it in `ThemeManager.AvailableThemes` (`Services/ThemeManager.cs`).
4. Rebuild. Your theme now appears in **View → Theme** and can be selected
   per-project via the `themes` section of a `.nstudio` file.

You should not need to touch any component's XAML or code-behind to do this
— if you find yourself doing so, that component has a hardcoded value it
shouldn't (see NFC-001 §6.1), and it's worth filing as a defect.
