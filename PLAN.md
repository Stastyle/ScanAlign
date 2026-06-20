# ScanAlign — Product & Engineering Plan

> Working name: **ScanAlign** (alternatives: *TrueAxis*, *Datum*, *ZeroPoint*).
> A Windows desktop app that takes raw 3D scan files (OBJ / PLY / STL) and snaps them
> onto the world XYZ axes and origin, so they drop into Fusion 360 (or any CAD) clean,
> centered, and square — every time.

---

## 1. The problem

When a scan comes straight off the scanner into Fusion 360 it is:

- **Off-origin** — the body floats somewhere far from (0,0,0).
- **Rotated arbitrarily** — no face is parallel to a plane, no edge is parallel to an axis.
- **Mis-scaled or unit-ambiguous** — mm vs m vs inch surprises.

Fixing this *inside* Fusion is slow and fiddly (Align, Move/Copy, construction planes, ground-to-plane).
The job is really a **rigid-body alignment problem**: find a rotation + translation (and optional uniform scale)
that maps scanner space → a clean, intentional CAD datum, then re-export.

**ScanAlign does exactly that one job, extremely well.**

---

## 2. Who it's for & the core loop

**User:** Anyone going from a 3D scanner → CAD/print. Reverse-engineering parts, fixturing, 3D-printing
replacement parts, QA. Comfortable with CAD concepts (planes, axes, origin) but wants this to take 60 seconds.

**The 60-second loop:**

```
Import  →  Pick alignment tool  →  Set datums on the mesh  →  Preview snap  →  Export aligned file
 (drag)      (3-point plane…)        (click points/holes)       (live)         (OBJ/PLY/STL)
```

Everything in the UI exists to make that loop fast, reversible, and obvious.

---

## 3. Design principles

1. **Non-destructive.** We never bake geometry until export. Alignment is a single 4×4 transform layered
   on top of the original mesh. Undo/redo always available; "reset to imported" always one click away.
2. **Show the math, don't hide it.** When the user fits a plane, show the RMS error. When they align two holes,
   show the residual angle. Trust comes from numbers.
3. **Live preview.** Every datum the user sets updates the proposed transform and the ghosted "after" position
   in real time. Commit is explicit.
4. **One job, done right.** v1 resists scope creep. Alignment + measurement only. Architecture leaves clean
   seams for everything else (see §10).

---

## 4. Technology stack (decided)

| Layer | Choice | Why |
|---|---|---|
| Language / runtime | **C# / .NET 8** | Native Windows, fast, single-publish `.exe`, strong tooling. |
| UI framework | **WPF** (not WinUI 3) | Mature 3D story, dockable-panel libraries, stable. WinUI 3's 3D path is still thin. |
| 3D viewport | **HelixToolkit.SharpDX** (DirectX 11) | Handles multi-million-triangle meshes via GPU. (WPF's built-in `Viewport3D` is too slow for scans.) |
| Geometry / mesh math | **geometry3Sharp (g3)** | Mesh data structures, plane fits, AABB, decimation, ray-casts — pure C#, MIT. |
| Linear algebra | **System.Numerics** (`Vector3`, `Matrix4x4`, `Quaternion`) + **Math.NET Numerics** for SVD/PCA | Core transforms in System.Numerics; SVD for best-fit planes/lines. |
| Mesh I/O | **AssimpNet** (OBJ/STL) + a dedicated **PLY** reader/writer (ASCII + binary, vertex colors, point clouds) | Assimp is broad but weak on PLY point clouds; own PLY parser guarantees round-trips. |
| Architecture | **MVVM** (CommunityToolkit.Mvvm) | Clean separation; viewport ↔ panels ↔ core stay decoupled. |
| Packaging | **MSIX** or single-file self-contained publish | Easy install/update; no runtime prerequisite for the user. |
| Tests | **xUnit** + golden-file fixtures | Geometry math is the risky part — it gets the most tests. |

> **Why not Electron/Python:** you chose native, and it's the right call here — scan meshes are big, and
> a GPU-backed native viewport with a real geometry library beats a browser canvas for this workload.

---

## 5. Architecture

Three projects in one solution, dependencies point inward (Core knows nothing about UI):

```
ScanAlign.sln
├─ ScanAlign.Core        ← no UI refs. Pure geometry, I/O, alignment solvers.
│   ├─ IO/               OBJ, PLY, STL readers/writers; unit detection
│   ├─ Model/            SceneObject, MeshData, PointSet, Transform stack
│   ├─ Solvers/          PlaneFit, LineFit, HoleDetect, PcaAlign, Icp (later)
│   ├─ Alignment/        AlignmentTool base + concrete tools (strategy pattern)
│   └─ Measure/          Distance, Angle, BoundingBox, RmsReport
├─ ScanAlign.App         ← WPF, MVVM. Viewport, panels, gizmo, commands.
│   ├─ ViewModels/
│   ├─ Views/            MainWindow, panels (left rail, inspector, status bar)
│   ├─ Viewport/         HelixToolkit host, picking, overlays, axis triad
│   └─ Interaction/      tools mediate clicks → Core solvers
└─ ScanAlign.Tests       ← xUnit; golden meshes; transform/round-trip assertions
```

**Key data model:** a `SceneObject` holds the immutable imported `MeshData` plus an ordered
**transform stack** (each alignment step is one rigid transform with provenance — "3-point plane to Z, 0.04mm RMS").
The composite world transform = product of the stack. Export bakes it. Undo = pop the stack.

**Alignment as a strategy.** Every tool implements:

```csharp
interface IAlignmentTool {
    string Name { get; }
    int RequiredPicks { get; }          // points/holes/etc. needed
    AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target);
    // target = which axis/plane/origin the result maps onto
}
```

A proposal returns `{ Matrix4x4 transform, double residual, string explanation }`.
New tools = new classes. The UI discovers them from a registry — no UI rewrites to add a tool.

---

## 6. The alignment toolset

### v1 tools (ship these)

| Tool | User does | Math | Maps to |
|---|---|---|---|
| **3-point plane** | Clicks 3 points on a flat face | Plane through 3 pts (or best-fit if >3) | Face → chosen world plane (XY/XZ/YZ); normal → axis |
| **Best-fit plane (N points / paint)** | Clicks/brushes many points on a face | PCA/SVD plane, shows RMS | Same, but robust on noisy scans |
| **2-hole / 2-point line** | Marks 2 holes (auto-center) or 2 points | Line through centers | Line → chosen axis (e.g. X) |
| **Point → origin** | Clicks one point | Translation only | That point → (0,0,0) |
| **Axis + plane combo** | One plane (2 DOF) + one line (1 DOF) | Constrain remaining rotation | Full 6-DOF lock from 2 datums |
| **PCA auto pre-align** | One click, no picks | Principal axes of the mesh → XYZ | Instant "good enough" starting pose |
| **Manual nudge gizmo** | Drag/rotate handles; type exact values | Direct transform edit | Fine-tuning after any tool |

**Why this set:** plane + line + origin are the three primitives that, combined, fully constrain a rigid body
(3 translation + 3 rotation DOF). Everything in v1 is a fast way to supply those constraints.

### Your "more ideas" — the expansion menu

- **Cylinder-axis align** — fit a cylinder to a shaft/bore; align its axis to an axis. Huge for mechanical parts.
- **Edge / silhouette align** — pick a straight edge, make it parallel to an axis.
- **Symmetry-plane detect** — auto-find the mirror plane of a symmetric part; align it to a world plane.
- **Two-plane intersection → axis** — pick two faces, their intersection line becomes an axis.
- **3-2-1 fixturing** (the classic GD&T datum scheme): primary plane (3 pts) → secondary line (2 pts) →
  tertiary point (1 pt). Fully deterministic, machinist-friendly. *Strong candidate for v1.1.*
- **Align-to-reference (ICP)** — load a reference CAD/mesh; iterative-closest-point snaps the scan to it.
- **Min-bounding-box align** — orient so the tightest oriented box is axis-aligned (great for printing/nesting).
- **Ground / drop-to-floor** — lowest plane sits on Z=0.
- **Mirror / flip** — fix handedness from some scanners.
- **Sketch-line align** — draw a 2D line on a projected silhouette and align to it.

### Targeting & origin control

A small "**Snap to**" control sets where the result lands:
- **Axis target:** +X / +Y / +Z (and which world plane for plane tools).
- **Origin policy:** keep / move-to-bbox-center / move-to-picked-point / move-to-plane-origin.
- **Up-axis convention:** Z-up (Fusion default) vs Y-up toggle, remembered per session.

---

## 7. Measurement tools (v1)

Trust + utility. All read-only, all overlay in the viewport:

- **Point-to-point distance** (with X/Y/Z component breakdown).
- **Hole / circle diameter** — fit a circle to picked rim points.
- **Plane-fit RMS** — quality of any plane datum.
- **Angle between two datums** (lines or planes).
- **Bounding box readout** — overall L×W×H, live in the status bar.
- **Coordinate probe** — hover/click shows world XYZ of a point.
- **Units & scale** — detect/assert mm·cm·m·inch; optional uniform rescale on import or export.

---

## 8. File I/O

| Format | Read | Write | Notes |
|---|---|---|---|
| **OBJ** | ✓ | ✓ | Mesh + optional normals; ignore materials in v1 (pass through if cheap). |
| **PLY** | ✓ | ✓ | ASCII **and** binary; vertices, faces, **vertex colors**, point clouds (no faces). |
| **STL** | ✓ | ✓ | Binary + ASCII; the Fusion/printing handoff format. |

- **Round-trip fidelity:** export preserves vertex count/order, colors, and normals where the format allows.
  Golden-file tests assert load→save→load equality.
- **Big-file strategy:** stream-parse; show a progress bar; cap initial render with optional decimation
  for display only (full-res kept for export).
- **Unit detection:** heuristic from bounding-box magnitude + a confirm dialog; never silently rescale.
- **Export writes the baked transform** and embeds provenance as a comment header (the alignment recipe).

*Deferred (architecture-ready): point-cloud formats (XYZ/PCD/E57), 3MF, and a mesh→solid bridge for Fusion.*

---

## 9. UI / UX & the look

**Overall feel:** calm, technical, dark-default (easy on the eyes for 3D work), single window, no clutter.
Think "CAD utility" — precise, gray-neutral chrome, one accent color for active datums.

**Layout — single window, four zones:**

```
┌─ Top toolbar ───────────────────────────────────────────────────────────┐
│ Open · Save  |  Undo Redo  |  View: ⌖ front top iso  |  Z-up ▾  |  Reset  │
├──────────────┬──────────────────────────────────────────┬────────────────┤
│ LEFT RAIL    │            3D VIEWPORT                    │  INSPECTOR     │
│ (tools)      │                                          │  (right panel) │
│              │     • axis triad + ground grid           │                │
│ Alignment    │     • the mesh (orig = ghost,            │  Selected tool │
│  ▸ 3-pt plane│       aligned = solid preview)           │  Picks: P1 P2  │
│  ▸ Best-fit  │     • datum markers + gizmo              │  Plane RMS:    │
│  ▸ 2 holes   │     • measurement overlays               │   0.041 mm     │
│  ▸ Point→0   │                                          │  Transform:    │
│  ▸ PCA auto  │                                          │   tx ty tz     │
│ Measure      │                                          │   rx ry rz     │
│  ▸ Distance  │                                          │  [Snap to ▾]   │
│  ▸ Diameter  │                                          │  [Commit]      │
├──────────────┴──────────────────────────────────────────┴────────────────┤
│ Status bar:  x 12.40  y -3.10  z 0.00 mm   |   BBox 84×40×22 mm   |  1.2M △│
└───────────────────────────────────────────────────────────────────────────┘
```

**Interaction details:**
- **Left rail** = the workflow. Pick a tool → it tells you what to click ("Click 3 points on a flat face").
  A small step indicator shows pick progress (●●○).
- **Viewport** = the truth. Original mesh dims to a ghost; the live "after" pose previews in solid.
  Datum markers are numbered, colored, draggable. An always-on axis triad + ground grid give orientation.
- **Inspector** = the numbers + the commit. Shows residuals, the proposed 4×4 (as friendly tx/ty/tz, rx/ry/rz),
  the "Snap to" target, and the **Commit** button. Below it, the **alignment stack** (history of committed steps,
  each removable).
- **Status bar** = live coordinate probe, bounding box, triangle count, units.
- **Keyboard:** number keys select tools, `Esc` cancels a pick, `Ctrl+Z/Y` undo/redo, `F` frame-selection,
  `1/2/3` standard views.
- **Empty state:** big drop-target — "Drag a scan here (OBJ · PLY · STL)".

**Theme:** dark gray chrome (#1f2124-ish), white-on-dark text, **one accent** (teal) for active datums/preview;
red/amber reserved for residual warnings ("RMS 2.1mm — face may not be flat"). Light theme available.

---

## 10. Roadmap (phased)

**Phase 0 — Skeleton (foundation)**
Solution + 3 projects, MVVM shell, HelixToolkit viewport that loads & orbits a mesh, axis triad + grid,
OBJ import. *Exit: I can drag in an OBJ and orbit it.*

**Phase 1 — I/O & model**
PLY + STL read/write, unit detection, transform stack, undo/redo, reset, export with provenance.
Golden-file round-trip tests. *Exit: load any of the 3 formats, move it, export, reload — identical.*

**Phase 2 — Core alignment (the product)**
Picking in viewport; 3-point plane, best-fit plane, point→origin, 2-point/2-hole line, PCA auto, manual gizmo;
"Snap to" targeting; live preview + commit. *Exit: a real scanner OBJ → aligned to XYZ in <60s.*

**Phase 3 — Measurement & polish**
Distance, diameter, angle, RMS, bbox, coordinate probe; status bar; warnings on bad fits; theming;
installer (MSIX). *Exit: shippable v1.*

**Phase 4+ — Expansion** (see §6 & §11), prioritized by demand.

---

## 11. Built-to-expand (design seams)

The architecture is deliberately shaped so these never require a rewrite:

- **New alignment tools** → add an `IAlignmentTool`; it auto-appears in the rail via the registry.
- **New formats** → add an `IMeshReader/Writer`; the import/export dialogs read the registry.
- **Heavier solvers (ICP, cylinder/symmetry fit)** → drop into `Core/Solvers`, no UI coupling.
- **Hole/feature auto-detection** → a `FeatureDetector` service feeding datums to any tool.
- **Batch mode & alignment recipes** → since each alignment is a serializable transform-stack with provenance,
  a recipe is just that stack replayed on new files; a headless `ScanAlign.Cli` can run it over a folder.
- **Fusion 360 add-in / plugin** → Core is pure .NET; a thin add-in can call the same solvers in-process later.
- **Reference-based QA / deviation color map** → measurement layer already overlays on the mesh; add a
  distance-to-reference heatmap.
- **Cross-platform** → Core has zero Windows deps; an Avalonia front-end could target Mac/Linux if ever needed.

---

## 12. Risks & decisions to watch

| Risk | Mitigation |
|---|---|
| Big meshes (10M+ △) stutter | SharpDX GPU rendering; display-only decimation; stream parsing. |
| Robust picking on dense/noisy meshes | Ray-cast via g3 spatial index; snap-to-vertex; best-fit tools tolerate noise. |
| PLY format zoo (ascii/binary/endian/custom props) | Own parser with a broad fixture corpus; fail loud, never silently drop data. |
| Hole detection reliability | v1: user marks the hole region, we fit the circle (assisted, not magic). Full auto later. |
| Unit ambiguity causing 25.4× / 1000× errors | Detect + **confirm** dialog; never auto-rescale silently. |
| Scope creep | v1 = alignment + measurement only; everything else is §11, behind the seams. |

---

## 13. First concrete steps

1. `dotnet new` the solution + 3 projects; add HelixToolkit.SharpDX, g3, Math.NET, AssimpNet, CommunityToolkit.Mvvm.
2. Phase 0 viewport: load an OBJ, orbit, triad + grid, dark theme shell.
3. PLY/STL I/O + round-trip tests (Phase 1) — the riskiest plumbing, done early.
4. First alignment tool end-to-end: **3-point plane → Z** with live preview + commit (proves the whole pipeline).
5. Iterate the remaining v1 tools against a folder of your real scanner exports.

---

*This document is the north star. v1 is intentionally small; the architecture is intentionally not.*
