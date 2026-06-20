# ScanAlign

A Windows desktop app that takes raw 3D scan files (OBJ · PLY · STL) and aligns them to the world
XYZ axes and origin, so they drop into Fusion 360 (or any CAD) clean, centered, and square.

See [PLAN.md](PLAN.md) for the product vision and [IMPLEMENTATION.md](IMPLEMENTATION.md) for the
build plan and architecture.

## Status

Functional v1. The full pipeline works end-to-end: load a scan → pick an alignment tool → set
datums → live preview → commit → export the aligned file. **66 tests pass**; `dotnet build` and
`dotnet test` are green.

## Requirements

- Windows 10/11
- .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)

## Build, test, run

```sh
dotnet build                                   # build everything
dotnet test                                    # run the test suite
dotnet run --project src/ScanAlign.App          # launch the app

# open a file straight away (also works by dragging a file onto the .exe):
dotnet run --project src/ScanAlign.App -- samples/tilted-cube.obj
```

A sample scan (a tilted, off-origin cube that mimics a raw scanner export) lives in
[samples/tilted-cube.obj](samples/tilted-cube.obj).

## How to align a scan

1. **Open** a scan (OBJ/PLY/STL), or pass it on the command line.
2. Pick a tool in the left rail:
   - **3-point plane** — click 3 points on a flat face to make it parallel to a world plane.
   - **Best-fit plane** — fit a face from many points (robust on noisy scans).
   - **2-point / 2-hole line** — align the line between two features to an axis.
   - **Point → origin** — move a picked point to (0,0,0).
   - **PCA auto** — one-click orient by principal axes.
3. Click points in the viewport (left-click). The inspector shows the live proposal, residual, and
   resulting transform; a translucent preview shows where it will land.
4. Set **Snap to** (target plane/axis + origin policy) in the inspector.
5. **Commit**. Repeat with more tools to fully constrain the part. **Undo/Redo/Reset** as needed.
6. **Export** — the alignment is baked into a new OBJ/PLY/STL with a provenance header.

**Viewport controls:** left-drag orbit · right/middle-drag pan · wheel zoom · left-click to add a datum.

## Architecture (short version)

- `ScanAlign.Core` — pure geometry, I/O, solvers, alignment tools, measurement. No UI dependency.
  Formats and tools are discovered by reflection, so adding one is just adding a class.
- `ScanAlign.App` — WPF (MVVM) shell, native WPF 3D viewport, orchestration (`SceneService`).
- `ScanAlign.Tests` — xUnit; golden geometry with known ground truth.

## Notable v1 decisions

- **Dependency-lean:** hand-written OBJ/PLY/STL parsers (no Assimp); math via MathNet; no
  geometry3Sharp. The 3D viewport uses **native WPF Media3D** (built into the Windows Desktop SDK)
  with a hand-written orbit camera — no external 3D dependency. A future perf pass can swap in a GPU
  backend behind `IViewportController` for very large meshes.
- **Non-destructive:** alignment is a stack of rigid transforms over the immutable imported mesh;
  nothing is baked until export.
