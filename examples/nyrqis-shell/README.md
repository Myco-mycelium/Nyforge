# Nyrqis Desktop Shell

Reference implementations of the Nyrqis desktop shell, designed in Nyforge.

## Files

| File | Description |
|------|-------------|
| `desktop-shell.nstudio` | **Full desktop shell** — Taskbar, StartMenu, Window system, Notifications, SystemTray, QuickSettings, LockScreen, PowerMenu, OSD |
| `desktop.nstudio` | Desktop surface with icons and wallpaper |
| `nyrqis-shell.nstudio` | Dashboard/overview workspace with status cards and event log |
| `security-center.nstudio` | Security monitoring workspace |
| `vault-workspace.nstudio` | Vault/encryption workspace |
| `windows.nstudio` | Window management experiments |
| `widgets.nstudio` | Widget host experiments |

## Architecture

The desktop shell uses NUI Shell components:

```
DesktopSurface
├── WorkspaceSwitcher
│   └── DesktopIcons + WindowFrame(s)
├── NotificationCenter (right edge)
├── StartMenu (bottom-left, from Taskbar)
├── Taskbar (bottom)
│   ├── StartButton
│   ├── Search
│   ├── PinnedApps
│   ├── SystemTray
│   └── Clock
├── QuickSettings (overlay)
├── PowerMenu (overlay)
├── LockScreen (fullscreen overlay)
└── OSD (floating notification)
```

## Preview

Open `desktop-shell.nstudio` in Nyforge and press **F5** or click **▶ Preview**.

## Pipeline

```
Nyforge → .nstudio → validate → preview → render → export → Nyrqis runtime
```

## State

The shell manages these state variables:

- `startMenuOpen` — whether the Start Menu is visible
- `quickSettingsOpen` — whether Quick Settings panel is open
- `notificationsOpen` — whether Notification Center is open
- `lockScreenOpen` — whether the Lock Screen is active
- `activeWindowId` — currently focused window
- `currentTime` — system clock
- `batteryLevel`, `wifiConnected`, `volumeLevel` — system status
- `doNotDisturb`, `darkMode` — user preferences
