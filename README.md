# ScanAlign

**Alpha v0.1** · A Windows desktop app that takes raw 3D scan files (OBJ · PLY · STL) and aligns
them to the world XYZ axes and origin — so they drop into Fusion 360 (or any CAD) clean, centered,
and square.

> Import a scan straight from a scanner and it's off-origin and rotated at some arbitrary angle.
> ScanAlign fixes that in seconds: pick a feature, snap it to an axis or plane, export.

See [PLAN.md](PLAN.md) for the product vision, [IMPLEMENTATION.md](IMPLEMENTATION.md) for the
architecture, and [KNOWN_ISSUES.md](KNOWN_ISSUES.md) for current limitations.

## Status

Early **alpha**. The full pipeline works end-to-end — load → pick a tool → set datums → live
preview → commit → export — and is covered by **92 automated tests** (`dotnet build` and
`dotnet test` are green). Expect rough edges, especially around picking precision; see
[KNOWN_ISSUES.md](KNOWN_ISSUES.md).

## Requirements

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.8`)

## Build, test, run

```sh
dotnet build                                   # build everything
dotnet test                                    # run the test suite (92 tests)
dotnet run --project src/ScanAlign.App          # launch the app

# open a file straight away (also works by dragging a file onto the .exe):
dotnet run --project src/ScanAlign.App -- samples/tilted-cube.obj
```

A sample scan (a tilted, off-origin cube mimicking a raw scanner export) lives in
[samples/tilted-cube.obj](samples/tilted-cube.obj).

## Aligning a scan

1. **Open** a scan (OBJ/PLY/STL), or pass it on the command line.
2. Pick an **alignment tool** in the left rail:

   | Tool | What it does |
   |------|--------------|
   | **3-point plane** | Click 3 points on a flat face to make it parallel to a world plane. |
   | **Best-fit plane** | Click many points on a face — robust on noisy scans. |
   | **2-point / 2-hole line** | Align the line between two features to an axis. |
   | **Point → origin** | Move a picked point to (0,0,0). |
   | **Center → origin** | Click points around a circle/pocket; a fitted center (robust to clustering) moves to the origin. |
   | **Center-to-center line** | Two fitted centers → a line snapped to an axis. |
   | **3-2-1 fixturing** | Primary face + secondary edge + origin point — fully constrains the part. |
   | **PCA auto** | One-click orient by the part's principal axes. |
   | **Drop to floor** | Rest the lowest point on Z = 0. |

3. Click points on the model in the viewport. The inspector shows the live proposal, residual, and
   resulting transform; a translucent preview shows where it will land.
4. In the inspector set:
   - **Snap to** — which plane/axis to target (with a plain-language description of each).
   - **Origin** — where the part's datum lands.
   - **Place on axis/plane** — lie *on* the target vs only *parallel* to it.
   - **Flip X / Y / Z** — add a 180° turn about any axis if the part comes out facing the wrong way.
   - For **3-2-1**: a **Secondary edge → axis** picker.
5. **Commit** (or press Enter). Repeat with more tools to fully constrain the part.
   **Undo / Redo / Reset** as needed; each step is listed under **History**.
6. **Export** — the alignment is baked into a new OBJ/PLY/STL with a provenance header.

**Viewport:** left-drag orbit · right/middle-drag pan · wheel zoom · left-click to add a datum
(only the model is clickable — not the grid or axes). Axis colors follow Fusion: **X red, Y green,
Z blue**.

**Keyboard:** `Ctrl+O` open · `Ctrl+S` export · `Ctrl+Z/Y` undo/redo · `Enter` commit ·
`Esc` clear picks · `Del`/`Backspace` remove last pick.

## Architecture (short version)

- `ScanAlign.Core` — pure geometry, I/O, solvers, alignment tools, measurement. No UI dependency.
  Formats and tools are discovered by reflection, so adding one is just adding a class.
- `ScanAlign.App` — WPF (MVVM) shell, native WPF 3D viewport, orchestration (`SceneService`).
- `ScanAlign.Tests` — xUnit; golden geometry with known ground truth.

## License

[MIT](LICENSE) © 2026 Stas Meirovich. Free for personal and commercial use; you may modify and
redistribute it, provided the copyright notice is retained.

## Author

Created by **Stas Meirovich**.
