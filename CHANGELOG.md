# Changelog

## 1.0.0 — first public release

### Added
- Canvas-based crosshair rendering: design in canvas pixels (800×600, 1080p, native, …),
  rasterise into a canvas-sized buffer, then upscale to the display.
- `CanvasFit` modes: `stretch`, `fit`, `native`.
- Per-pixel alpha rendering through a hand-written `UpdateLayeredWindow` layer, with a
  colour-key fallback for drivers that cannot composite layered windows correctly.
- `Upscale` option: nearest (crisp) or bilinear (soft).
- `Glow` halo (0–100) for tunable edge softness.
- Separate horizontal/vertical arm length, thickness and gap; optional arm sync.
- Outline with "sides only" mode (no end caps, keeps the centre gap clean).
- Live settings window (`F6`) with colour picker and hex input.
- Visual preset library with live thumbnails (save / load / delete).
- Per-game profiles driven by the foreground process.
- Multi-monitor support, DPI awareness, tray icon, configurable global hotkeys.
- Command line tools: `--version`, `--selftest`, `--render`, `--snapshot`, `--geom`,
  `--profiles`, `--presets`.
- Bundled self-test suite (12 assertions) covering config round-trips, profile/preset
  parsing, pixel geometry, symmetry and antialiasing.
- `build.ps1` that compiles with the compiler shipped in the .NET Framework.

### Fixed
- The overlay window was full-screen and could cover the whole desktop when per-pixel
  transparency failed; it is now sized to the crosshair bounding box.
- Per-pixel alpha produced a black or magenta rectangle: the layered surface was never
  updated (WM_PAINT is not the paint path for layered windows) and the background was
  cleared with an opaque colour.
- Left/right arms could differ by one pixel due to independent rounding; geometry is now
  computed from a single centre in device pixels.
- Multiple instances could run at the same time; a named mutex now enforces a single one.
- `--render` still used the old `PixelSnap` flag to pick an interpolation mode.
