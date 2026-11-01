# Crosshair Overlay

[![build](https://github.com/lyc20050809/crosshair-overlay-/actions/workflows/build.yml/badge.svg)](https://github.com/lyc20050809/crosshair-overlay-/actions/workflows/build.yml)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey.svg)]()
[![dependencies](https://img.shields.io/badge/dependencies-none-brightgreen.svg)]()

A tiny, dependency-free crosshair overlay for Windows. It draws a crosshair on top of
any game or desktop app using a **resolution-independent canvas**: you design the
crosshair in the coordinate space of a chosen resolution (800×600, 1080p, native, …)
and the tool maps it to whatever your monitor actually shows.

No installer, no registry, no background service — one `.exe`, a few `.ini` files, done.

## Install

1. Download `CrosshairOverlay-1.0.0-win64.zip` from the [latest release](../../releases/latest).
2. Extract anywhere (e.g. `C:\Tools\CrosshairOverlay`).
3. Run `CrosshairOverlay.exe`. A crosshair appears; the tray icon holds the menu.
4. Press `F6` to configure, `F8` to show/hide, `Ctrl+Alt+Q` to quit.

Nothing is written outside the folder. Delete it to uninstall.

## Screenshots

> _Placeholder — add 2–3 images here (crosshair in game, `F6` settings window, preset library)._
>
> Suggested layout:
>
> | Crosshair | Settings (`F6`) | Preset library |
> |---|---|---|
> | `docs/img/crosshair.png` | `docs/img/settings.png` | `docs/img/presets.png` |


```
┌──────────────────────────────────────────┐
│  canvas 800×600  →  stretch  →  2560×1440 │
│  scaleX = 3.2     scaleY = 2.4            │
│  a 2×10 px arm becomes 6.4×24 px          │
└──────────────────────────────────────────┘
```

## Features

- **Canvas-based design** — the crosshair is authored in canvas pixels and upscaled, so
  it behaves like an in-game crosshair rendered at that resolution.
- **Two transparency modes** — per-pixel alpha (soft, true antialiasing on the outline)
  or color-key (maximum compatibility).
- **Tunable softness** — `Upscale` (nearest / bilinear) plus a `Glow` halo (0–100).
- **Live settings window** (`F6`) — every change applies instantly.
- **Visual preset library** — save named crosshairs, browse them as cards with live
  thumbnails, click to load, right-click to delete.
- **Per-game profiles** — bind a canvas + geometry to an executable and it switches
  automatically when that game gets focus.
- **Any shape** — cross, dot, ring, X, T; separate horizontal / vertical thickness and gap.
- **Multi-monitor, DPI aware**, single instance guard, tray icon, global hotkeys.

## Quick start

1. Download / build `CrosshairOverlay.exe`.
2. Double-click it. A crosshair appears and a tray icon shows up.
3. Press `F6` to open settings; `F8` hides/shows, `Ctrl+Alt+Q` quits.
4. For games: run them **borderless windowed** (exclusive fullscreen always hides overlays).

## Hotkeys

| Key | Action |
|---|---|
| `F6` | Settings window |
| `F7` | Cycle canvas presets |
| `F8` | Show / hide the crosshair |
| `F9` | Next color |
| `F10` | Reload `crosshair.ini` |
| `Ctrl+Alt+Q` | Quit |

All hotkeys are configurable in `crosshair.ini`.

## Configuration

`crosshair.ini` is written next to the exe; a documented example lives in
[`crosshair.example.ini`](crosshair.example.ini). The essentials:

```ini
Colors=#00FF00,#FF3B30
Shape=cross
Outline=1
OutlineMode=1        ; sides only (no end caps)
AlphaMode=0          ; 0 = color key, 1 = per-pixel alpha (needs restart)
PixelSnap=1          ; 1 = integer canvas pixels, 0 = allow sub-pixel
Upscale=0            ; 0 = nearest (crisp), 1 = bilinear (soft)
Glow=0               ; 0-100 halo, alpha mode only
CanvasWidth=800
CanvasHeight=600
CanvasFit=stretch    ; stretch | fit | native
CanvasPresets=800x600,1280x720,1920x1080,native
AnchorX=50
AnchorY=50
SizeX=6
SizeY=10
ThicknessX=2
ThicknessY=2
GapX=4
GapY=4
```

`presets.ini` holds named crosshairs, `profiles.ini` holds per-game overrides:

```ini
; presets.ini
[2K thin cross]
Canvas=2560x1440
Fit=stretch
Size=6,10
Thickness=2,2
Gap=4,4
Color=#00FF00
Shape=cross
Outline=1
OutlineMode=1
SyncArms=1
Alpha=255
Glow=30
Upscale=1

; profiles.ini
cs2.exe = 800x600|stretch|6,10|2,2|4,4
```

## Command line

The GUI build is a windows executable, so it has no console. For scripting, use
`CrosshairOverlay.CLI.exe` (built alongside) or any build with `/target:exe`:

```
CrosshairOverlay.CLI.exe --version
CrosshairOverlay.CLI.exe --selftest --config crosshair.ini
CrosshairOverlay.CLI.exe --render out.png --config crosshair.ini 240
CrosshairOverlay.CLI.exe --snapshot shot.png --config crosshair.ini
CrosshairOverlay.CLI.exe --geom --config crosshair.ini
CrosshairOverlay.CLI.exe --profiles --config crosshair.ini
CrosshairOverlay.CLI.exe --presets --config crosshair.ini
```

`--selftest` runs the bundled test suite (12 assertions: config round-trip, profile and
preset parsing, pixel geometry, symmetry, antialiasing) and exits non-zero on failure.

## Build

Requires only the .NET Framework 4.x compiler that ships with Windows — no SDK, no NuGet.

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Outputs `CrosshairOverlay.exe` (GUI) and `CrosshairOverlay.CLI.exe` (console) in the repo
root. The whole application is a single source file: [`src/Program.cs`](src/Program.cs).

## How the canvas works

```
scaleX = screenWidth  / canvasWidth
scaleY = screenHeight / canvasHeight
screen length = canvas length × scale
```

`CanvasFit` decides how the canvas is mapped:

- `stretch` — fills the screen, X and Y scale independently (mimics a 4:3 image stretched to 16:9).
- `fit` — keeps the aspect ratio and letterboxes.
- `native` — canvas equals the screen (1:1 pixels).

The crosshair is rasterised into a canvas-sized buffer first and then upscaled, so it is
quantised to the canvas pixel grid exactly like a game rendering at that resolution.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Nothing on screen over a game | Use borderless windowed; exclusive fullscreen hides overlays |
| A black or magenta box around the crosshair | Switch `AlphaMode` back to `0`; some systems/drivers fail per-pixel alpha |
| Crosshair invisible | Press `F8` (hidden), check the tray icon, make sure only one instance runs |
| Hotkey does nothing | Another app grabbed it — change `KeyToggle` etc. in the ini |
| Too blurry / too sharp | `Upscale=nearest` + higher canvas resolution for sharp; `Upscale=bilinear` + `Glow` for soft |

## Disclaimer

This tool draws a window on top of your screen. It does **not** inject into games, read
game memory, or modify game files. That said, some online games treat any overlay as
suspicious — use it in single-player titles, or check the game's rules first. You are
responsible for how you use it.

## License

MIT — see [LICENSE](LICENSE).

---

中文文档：[README.zh-CN.md](README.zh-CN.md) · 详细手册：[docs/使用手册.md](docs/使用手册.md) ·
发布指南：[docs/如何发布到GitHub.md](docs/如何发布到GitHub.md) · Detailed manual: [docs/MANUAL.md](docs/MANUAL.md)
