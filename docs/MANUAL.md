# Crosshair Overlay — User Manual

Version 1.0.0

## 1. What it is

A standalone Windows overlay that renders a crosshair on top of other windows. It is
built around one idea: **a crosshair should be designed at a resolution, not at a pixel
size**. You pick a canvas (800×600, 1080p, native, …), design inside it, and the tool
maps that canvas onto your monitor.

It never installs anything, never touches the registry, and does not inject into other
processes. Delete the folder to uninstall.

## 2. Installation

1. Copy `CrosshairOverlay.exe` anywhere (e.g. `C:\Tools\CrosshairOverlay\`).
2. Double-click it. Windows SmartScreen may warn about an unsigned binary — choose
   *More info → Run anyway*.
3. A crosshair appears and an icon is added to the notification area.

The app creates `crosshair.ini` next to the executable on first run. `presets.ini` and
`profiles.ini` are created on demand.

### Requirements

- Windows 10/11, 64-bit
- .NET Framework 4.x (preinstalled on Windows 10/11)
- A GPU/driver that supports layered windows if you want per-pixel alpha

## 3. First run

| Step | What to do |
|---|---|
| 1 | Press `F6` to open the settings window |
| 2 | Pick a canvas (the default `800×600`, or `native` for 1:1 pixels) |
| 3 | Choose colour, shape, arm length, thickness, gap |
| 4 | Click **Save config** — settings are persisted to `crosshair.ini` |
| 5 | Press `F8` to hide/show, `Ctrl+Alt+Q` to quit |

Changes are applied live; nothing is written to disk until you click **Save config**.

## 4. The canvas

The crosshair is described in **canvas pixels**. At render time:

```
scaleX = screenWidth  / canvasWidth
scaleY = screenHeight / canvasHeight
```

Example — canvas 800×600 on a 2560×1440 display (`stretch`):

| Canvas | Scale | On screen |
|---|---|---|
| horizontal arm 6 px | × 3.2 | 19.2 px |
| vertical arm 10 px | × 2.4 | 24 px |
| line 2 px | × 3.2 / × 2.4 | 6.4 × 4.8 px |

### CanvasFit

| Value | Behaviour | Use when |
|---|---|---|
| `stretch` | fills the screen, X and Y scale independently | the game stretches a 4:3 image to 16:9 |
| `fit` | keeps aspect ratio, letterboxes | the game keeps its aspect ratio |
| `native` | canvas = screen, 1:1 | you want a fixed screen-space crosshair |

### Why the crosshair is rasterised, not vector-scaled

The renderer draws the crosshair into a canvas-sized buffer first and then upscales the
whole buffer. That quantises the shape to the canvas pixel grid — exactly what a game
rendering at that resolution does. `Upscale` then decides how the buffer is enlarged:

- `nearest` (default) — hard blocks, crisp
- `bilinear` — smooth, softer, can look mushy at large scale factors

## 5. Settings window (`F6`)

### Canvas group

| Control | Meaning |
|---|---|
| Preset | fill canvas width/height from a list |
| Width / Height | canvas resolution |
| Mode | `stretch` / `fit` / `native` |
| Pixel snap | round geometry to whole canvas pixels |
| Upscale | nearest / bilinear |
| Anchor X / Y | crosshair position inside the canvas (50/50 = centre) |
| Per-pixel alpha | switch transparency mode (needs restart) |

### Crosshair group

| Control | Meaning |
|---|---|
| Shape | cross / dot / circle / x / t |
| Colour | picker or `#RRGGBB` |
| Arm X / Y | horizontal / vertical arm length (canvas px) |
| Thickness X / Y | vertical-line width / horizontal-line height (canvas px) |
| Gap X / Y | space around the centre |
| Outline | outline width; **Sides only** outlines the long sides without end caps |
| Opacity | 0–255 |
| Sync arms | mirror X values onto Y |
| Soft edge (dither) | legacy 1-bit soft edge, useful only in colour-key mode |
| Glow | 0–100 halo strength, per-pixel alpha only |

### Bottom buttons

| Button | Action |
|---|---|
| Save config (Enter) | write `crosshair.ini` |
| Save for current game | write a `profiles.ini` entry for the foreground process |
| Preset library | open the visual preset browser |
| Close (Esc) | close the window, keep the overlay running |
| Quit | exit the application |

## 6. Preset library

`Preset library` opens a card view:

- each card shows a live thumbnail rendered with the same code path as the overlay,
  the preset name, and its canvas;
- **left click** loads, **right click** loads or deletes;
- type a name at the bottom and click **Save current crosshair as preset**.

Presets live in `presets.ini` as named sections and are safe to edit by hand.

## 7. Per-game profiles

`profiles.ini` maps a process name to a canvas + geometry:

```
cs2.exe = 800x600|stretch|6,10|2,2|4,4
```

Format: `canvasW x canvasH | fit | sizeX,sizeY | thicknessX,thicknessY | gapX,gapY`.

Every 0.8 s the tool checks the foreground process; when it matches, the profile is
applied. Switching to an unprofiled application restores the global `crosshair.ini`
values.

You rarely need to edit this file by hand: open the game, tune the crosshair, then press
**Save for current game** in the settings window.

## 8. Command line

The GUI binary has no console; use `CrosshairOverlay.CLI.exe` for these:

| Command | Purpose |
|---|---|
| `--version` | print the version |
| `--selftest` | run the test suite, exit code 0 = pass |
| `--render out.png [crop]` | render the current crosshair to a PNG (diagnostics) |
| `--snapshot shot.png` | capture a 160×160 screen region around the crosshair |
| `--geom` | print window bounds and transparency mode |
| `--profiles` | list parsed per-game profiles |
| `--presets` | exercise the preset store (save/apply/delete) |

## 9. Building and testing

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
CrosshairOverlay.CLI.exe --selftest --config crosshair.ini
```

The build uses the C# compiler bundled with the .NET Framework; there are no external
dependencies. The test suite covers configuration round-tripping, profile and preset
parsing, pixel geometry, symmetry and antialiasing.

## 10. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Nothing appears over a game | Game is in exclusive fullscreen — switch to borderless windowed |
| Magenta or black rectangle | `AlphaMode=1` not supported by the driver; set `AlphaMode=0` |
| Crosshair invisible after changing a mode | Restart the app (window styles are fixed at creation) |
| Two crosshairs | Several instances running — only one is allowed now, quit the old ones |
| Hotkey ignored | Taken by another application; change the key in `crosshair.ini` |
| Blurry | Lower the canvas upscale factor (`Upscale=0`), enlarge the canvas, reduce `Glow` |
| Halo too strong | Set `Glow` to 20–30, or 0 |

## 11. Limits

- Exclusive-fullscreen games cannot be covered by any overlay; use borderless windowed.
- The tool cannot detect a game's internal render resolution (that would require
  injecting into the process), so the canvas is chosen manually and remembered per game.
- Per-pixel alpha depends on the desktop compositor; a few drivers render layered windows
  incorrectly. Colour-key mode is the fallback.
