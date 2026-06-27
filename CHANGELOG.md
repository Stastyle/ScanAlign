# Changelog

All notable changes to ScanAlign are documented here.

## [0.1-alpha-1] — 2026-06-28

First public alpha.

### Added

- **Mesh I/O** for OBJ, PLY (ASCII + binary, little/big-endian, vertex colors, point clouds), and
  STL (binary + ASCII, with vertex welding). Round-trip tested.
- **Alignment tools** (auto-discovered by reflection): 3-point plane, best-fit plane,
  2-point / 2-hole line, point → origin, **center → origin** and **center-to-center line**
  (clustering-robust circle-fit centers), **3-2-1 fixturing**, PCA auto, and drop-to-floor.
- **Targeting controls**: snap-to plane/axis with plain-language descriptions, origin placement,
  **parallel vs on-axis** placement, independent **Flip X / Y / Z**, and a secondary-axis picker for
  3-2-1.
- **Geometry solvers**: best-fit plane/line/circle (with RMS), PCA aligner, bounding box.
- **Measurement core**: distance, angle, hole diameter, bounding box (not yet surfaced in the UI).
- **Heuristic unit detection** from bounding-box magnitude.
- **WPF app**: dark MVVM shell, native WPF 3D viewport (orbit/pan/zoom, ground grid, Fusion-style
  X/Y/Z axes + legend, surface/point-cloud rendering, translucent preview, datum + center markers),
  non-destructive alignment stack with undo/redo/reset/history, keyboard shortcuts, and export with a
  provenance header.

### Notes

- This is an early alpha; see [KNOWN_ISSUES.md](KNOWN_ISSUES.md).
- 92 automated tests; `dotnet build` and `dotnet test` are green.
