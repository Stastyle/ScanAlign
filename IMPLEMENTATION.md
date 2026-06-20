# ScanAlign — Implementation Plan (parallel-agent edition)

> **Status: v1 complete ✅** — all waves built sequentially in one session. Core (I/O, solvers,
> alignment tools, measurement), orchestration, and the WPF app are done; `dotnet build` and
> `dotnet test` are green (**66 tests**). The app launches and runs the full load→align→export loop.
> See [README.md](README.md) to build and run.
>
> **One deviation from §0:** the 3D viewport uses **native WPF Media3D** (not HelixToolkit). The
> classic HelixToolkit.Wpf ships only a .NET Framework target (unusable on net8), and the SharpDX
> variant's API couldn't be verified without a GPU/display in this environment. Native Media3D has
> zero external 3D dependency and runs anywhere WPF runs; a GPU backend can be swapped in later
> behind `IViewportController`. The **picking-brush for best-fit-plane** and the **manual nudge
> gizmo** (T11) are the remaining viewport niceties — point-picking, orbit/pan/zoom, preview, and
> markers all work today.

Companion to [PLAN.md](PLAN.md). This document breaks the build into **tracks that can run in
parallel without colliding**. The strategy: one blocking foundation wave freezes every shared
contract, then independent agents each own a **disjoint set of files** and code against those
frozen interfaces.

---

## 0. The rules that keep agents from breaking each other

These are non-negotiable conventions. Follow them and two agents never touch the same file.

1. **Contracts are frozen in Wave 0.** All shared types, interfaces, enums, and data models live in
   `ScanAlign.Core` and are created *first*, by *one* agent. After Wave 0 merges, **no track may change a
   contract signature** without raising it as an explicit "contract change" (rare, reviewed, serialized).
2. **One file = one owner.** The §6 ownership map assigns every path to exactly one track. An agent may
   only create/edit files inside its owned paths. Need something outside? It already exists as a contract,
   or you file a contract-change request.
3. **No shared central files to edit.** We design *away* the usual collision points:
   - **No `.csproj` edits after Wave 0.** Projects are SDK-style (source files are globbed automatically —
     adding a `.cs` file needs zero project edits). All NuGet packages are declared once in
     `Directory.Packages.props` during Wave 0.
   - **No central registry edits.** Readers, writers, solvers, and alignment tools are discovered by
     **assembly reflection** (scan for `IMeshReader`, `IAlignmentTool`, …). Adding a tool = adding a file.
   - **No central DI/composition edits.** The composition root uses the same assembly-scanning so a new
     service self-registers by implementing its interface + a marker attribute.
   - **No shared XAML.** `MainWindow.xaml` is frozen in Wave 1 with empty region placeholders; each panel
     is a self-contained `UserControl` in its own files. Theme lives in one resource dictionary owned by the
     App-foundation track; panels *reference* keys, never edit it.
4. **One git worktree/branch per track.** `git init` the repo, then each agent works in its own
   `git worktree` on branch `track/<name>`. Integration happens by merging green branches in wave order.
   Because file ownership is disjoint, merges are conflict-free by construction.
5. **Every track is test-first and self-verifying.** Each track adds its own test files to
   `ScanAlign.Tests` (also SDK-globbed — no project edit). A track is "done" when `dotnet test` is green
   *and* its public contract is fully implemented (no `NotImplementedException` left in its scope).
6. **The solution always builds green.** Wave 0 ships fully-implemented concrete model types
   (`TransformStack`, `SceneObject`) and registries; everything else is interfaces + records, so there are no
   throwing stubs to trip over. Tracks add their implementation classes; the build stays green throughout.

### Two gotchas Wave 0 already hit (so you don't have to)

- **`Plane` name clash.** Our `ScanAlign.Core.Model.Plane` collides with `System.Numerics.Plane`. Core resolves
  it with a global alias in `GlobalUsings.cs` (`global using Plane = ScanAlign.Core.Model.Plane;`). **Any other
  assembly** (App, Tests) that imports both `System.Numerics` and `ScanAlign.Core.Model` must add the same alias
  to its own `GlobalUsings.cs`.
- **WPF projects omit `System.IO` from implicit usings.** The Windows Desktop SDK (`UseWPF=true`, i.e. the App
  and Tests projects) uses a different implicit-usings set that does **not** include `System.IO`. Add
  `global using System.IO;` to those projects' `GlobalUsings.cs` (already done for Tests).

---

## 1. Dependency waves (the parallelism map)

```
WAVE 0  ── Core Foundation (1 agent, BLOCKING) ───────────────────────────────┐
            solution, projects, packages, ALL contracts, stubs, registries,     │
            test harness, sample mesh fixtures                                   │
                                                                                │ merge
WAVE 1  ── runs fully in parallel ───────────────────────────────────────────┐ ▼
   Core:  T1 OBJ I/O · T2 PLY I/O · T3 STL I/O · T4 Plane/Line solvers          │
          T5 Circle/Hole/PCA/BBox solvers · T13 Unit detection                  │
   App:   B  App Foundation (shell, MVVM, DI, theme, frozen MainWindow regions, │
             empty HelixToolkit host, ViewModel + service contracts)            │
                                                                                │ merge
WAVE 2  ── parallel, needs Wave-1 results ────────────────────────────────────┤
   Core:  T6 Plane alignment tools · T7 Line/Origin/PCA alignment tools         │
          T8 Measurement tools                                                  │
   App:   T10 Viewport rendering (mesh, triad, grid, camera)                    │
          T12 Panels (tool rail, inspector, status bar)                         │
                                                                                │ merge
WAVE 3  ── parallel, needs Wave-2 results ────────────────────────────────────┤
   App:   T11 Viewport interaction (picking, datum markers, manual gizmo)       │
          T14 Scene orchestration wiring (load→pick→solve→preview→commit)       │
                                                                                │ merge
WAVE 4  ── Integration & ship (1–2 agents) ───────────────────────────────────┘
          end-to-end glue, MSIX installer, perf pass, docs
```

**Agent budget:** Wave 1 supports up to **7 agents at once** (T1,T2,T3,T4,T5,T13,B). Wave 2 supports **5**.
You never need all at once — pick however many you want to run; tracks are independent within a wave.

---

## 2. WAVE 0 — Core Foundation (single agent, blocking)

Everything downstream depends on this. Deliver it, merge it, *then* fan out.

### 2.1 Solution scaffold
- `ScanAlign.sln` with three SDK-style projects targeting **.NET 8**:
  `ScanAlign.Core` (classlib), `ScanAlign.App` (WPF, `net8.0-windows`, `UseWPF=true`),
  `ScanAlign.Tests` (xUnit).
- **Central package management:** `Directory.Packages.props` declaring *all* versions up front.
  **Final lean set (decided during Wave 0):**
  `MathNet.Numerics` (Core math), `HelixToolkit.SharpDX.Core.Wpf` + `CommunityToolkit.Mvvm` +
  `Microsoft.Extensions.DependencyInjection` (App), `Microsoft.NET.Test.Sdk` + `xunit` +
  `xunit.runner.visualstudio` (Tests).
  **Dropped from the original sketch** to keep the dependency surface lean and robust:
  *AssimpNet* (OBJ/STL get hand-written parsers — simple formats, removes an unmaintained native dep),
  *geometry3Sharp* (picking uses HelixToolkit's built-in hit-testing; solvers use MathNet directly),
  *FluentAssertions* (v8 is commercially licensed — tests use plain xUnit asserts).
- `Directory.Build.props`: `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true`.
- A build/test script (`build.ps1`) and a stub CI workflow.

### 2.2 The frozen contracts (the heart of Wave 0)

> These signatures are the API every track codes against. Bodies are stubs (`throw new NotImplementedException()`).
> Listed at the level needed to freeze the surface.

**Geometry primitives & model** — `ScanAlign.Core/Model/`
```csharp
public enum Unit { Unknown, Millimeter, Centimeter, Meter, Inch }

public sealed record MeshData(                      // immutable; the imported truth
    IReadOnlyList<Vector3> Vertices,
    IReadOnlyList<int>     Indices,                 // triangle list; empty => point cloud
    IReadOnlyList<Vector3>? Normals,
    IReadOnlyList<Vector4>? Colors,
    Unit Unit, string? SourcePath) {
    public bool IsPointCloud => Indices.Count == 0;
}

public readonly record struct Plane (Vector3 Point, Vector3 Normal);
public readonly record struct Line3 (Vector3 Point, Vector3 Direction);
public readonly record struct Circle3(Vector3 Center, Vector3 Normal, float Radius);
public readonly record struct Aabb  (Vector3 Min, Vector3 Max);

public enum DatumKind { Point, PlaneRegion, LineEndpoint, HoleCenter }
public sealed record Datum(DatumKind Kind, Vector3 Position,
                           Vector3? Normal = null,
                           IReadOnlyList<Vector3>? SupportPoints = null);

public enum TargetKind  { AxisX, AxisY, AxisZ, PlaneXY, PlaneXZ, PlaneYZ }
public enum OriginPolicy{ Keep, BBoxCenter, PickedPoint, PlaneOrigin }
public enum UpAxis      { Z, Y }
public sealed record AlignmentTarget(TargetKind Kind, OriginPolicy Origin, UpAxis Up);

public sealed record AlignmentProposal(Matrix4x4 Transform, double Residual,
                                       string Explanation, bool IsComplete);

public sealed record AlignmentStep(Matrix4x4 Transform, string Description,
                                   double Residual, DateTimeOffset At);

public sealed class TransformStack {                // non-destructive history
    public IReadOnlyList<AlignmentStep> Steps { get; }
    public Matrix4x4 Composite { get; }             // product of all steps
    public void Push(AlignmentStep step);
    public void Pop();
    public void Clear();
    public event EventHandler? Changed;
}

public sealed class SceneObject {
    public MeshData Original { get; }
    public TransformStack Stack { get; }
    public Matrix4x4 World => Stack.Composite;
}
```

**I/O contracts** — `ScanAlign.Core/IO/`
```csharp
public sealed record ReadOptions (IProgress<float>? Progress = null);
public sealed record WriteOptions(IProgress<float>? Progress = null, bool Binary = true,
                                  string? ProvenanceHeader = null);

public interface IMeshReader {
    IReadOnlyList<string> Extensions { get; }       // e.g. [".obj"]
    MeshData Read(Stream stream, ReadOptions options);
}
public interface IMeshWriter {
    IReadOnlyList<string> Extensions { get; }
    void Write(Stream stream, MeshData mesh, WriteOptions options);
}
public interface IUnitDetector { Unit Detect(MeshData mesh); }
```

**Solver contracts** — `ScanAlign.Core/Solvers/`
```csharp
public sealed record PlaneFitResult (Plane Plane,  double Rms);
public sealed record LineFitResult  (Line3 Line,   double Rms);
public sealed record CircleFitResult(Circle3 Circle,double Rms);

public interface IPlaneFitter   { PlaneFitResult  Fit(IReadOnlyList<Vector3> points); }
public interface ILineFitter    { LineFitResult   Fit(IReadOnlyList<Vector3> points); }
public interface ICircleFitter  { CircleFitResult Fit(IReadOnlyList<Vector3> rimPoints); }
public interface IPcaAligner     { Matrix4x4 PrincipalAxesToWorld(IReadOnlyList<Vector3> pts); }
public interface IBoundingBox    { Aabb Compute(IReadOnlyList<Vector3> pts); }
```

**Alignment tool contract** — `ScanAlign.Core/Alignment/`
```csharp
public interface IAlignmentTool {
    string Id { get; }                  // stable key, e.g. "three-point-plane"
    string Name { get; }                // UI label
    int    RequiredPicks { get; }
    DatumKind ExpectedDatum { get; }
    AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target);
}
```

**Measurement contracts** — `ScanAlign.Core/Measure/`
```csharp
public sealed record DistanceResult(float Distance, Vector3 Delta);     // Delta = per-axis components
public sealed record AngleResult(float Degrees);
public sealed record DiameterResult(float Diameter, double Rms);
public interface IMeasurements {
    DistanceResult  Distance(Vector3 a, Vector3 b);
    AngleResult     Angle(Line3 a, Line3 b);
    DiameterResult  Diameter(IReadOnlyList<Vector3> rimPoints);
    Aabb            BoundingBox(IReadOnlyList<Vector3> pts);
}
```

**Reflection registries** — `ScanAlign.Core/Registry/`
```csharp
public sealed class MeshFormatRegistry {            // scans assemblies for IMeshReader/IMeshWriter
    public IMeshReader? ReaderFor(string extension);
    public IMeshWriter? WriterFor(string extension);
    public IReadOnlyList<string> ReadableExtensions { get; }
    public IReadOnlyList<string> WritableExtensions { get; }
}
public sealed class AlignmentToolRegistry {         // scans assemblies for IAlignmentTool
    public IReadOnlyList<IAlignmentTool> Tools { get; }
    public IAlignmentTool? ById(string id);
}
```

### 2.3 Test harness + fixtures
- `ScanAlign.Tests` references both projects, with a `Fixtures/` folder containing tiny known meshes:
  a unit cube (OBJ/PLY/STL), a tilted plane with known normal, a plate with two holes of known centers,
  and a noisy point cloud. These are the golden inputs every Core track tests against.
- One smoke test asserting the solution builds and registries are non-empty.

**Wave 0 Definition of Done:** `dotnet build` and `dotnet test` green; every contract present as a stub;
registries discover the stub implementations; fixtures committed.

---

## 3. WAVE 1 — parallel tracks

Each runs independently against the frozen contracts. Owned paths in §6.

### T1 — OBJ I/O · `Core/IO/Obj*`
Read/write OBJ (vertices, faces, normals; materials passed-through or ignored). Implements `IMeshReader`,
`IMeshWriter`. **DoD:** round-trip test on cube fixture (load→save→load equal); handles large files via streaming.

### T2 — PLY I/O · `Core/IO/Ply*`
ASCII **and** binary (little/big-endian), vertices, faces, **vertex colors**, and face-less **point clouds**.
Own parser (don't rely on Assimp here). **DoD:** round-trips ASCII+binary fixtures incl. colors and a point cloud;
malformed-file test fails loud, never silently drops data.

### T3 — STL I/O · `Core/IO/Stl*`
Binary + ASCII STL. STL has no shared vertices/normals-per-vertex — implement vertex welding on read.
**DoD:** round-trip on cube fixture; welded vertex count correct.

### T4 — Plane & Line solvers · `Core/Solvers/PlaneFitter.cs, LineFitter.cs`
Best-fit plane (3 points exact; N points via SVD/PCA, returns RMS). Best-fit line via PCA. Uses Math.NET.
**DoD:** tilted-plane fixture recovers known normal within tolerance; RMS≈0 for planar input; line fit recovers axis.

### T5 — Circle/Hole, PCA align, BBox solvers · `Core/Solvers/CircleFitter.cs, PcaAligner.cs, BoundingBox.cs`
Circle fit to rim points (→ hole center + diameter + RMS). PCA principal-axes→world matrix. AABB.
**DoD:** two-hole fixture recovers both centers/diameters; PCA orients a known-skewed cloud; AABB exact.

### T13 — Unit detection · `Core/IO/UnitDetector.cs`
Heuristic `IUnitDetector` from bounding-box magnitude → suggested `Unit` (never auto-applies; just suggests).
**DoD:** classifies the fixtures' known scales correctly; returns `Unknown` when ambiguous.

### B — App Foundation · `App/*` skeleton (see §4)
Runs concurrently with all Core tracks (depends only on frozen Core contracts).

---

## 4. WAVE 1 (B) — App Foundation (single agent)

Freezes the **App contract surface** so Wave-2 UI tracks don't collide. Deliverables:

- **MVVM shell:** `App.xaml`/`App.xaml.cs`, composition root using `Microsoft.Extensions.DependencyInjection`
  with **assembly scanning** (services self-register via an `[AppService]` marker — no central edits later).
- **`MainWindow.xaml` frozen with empty regions:** named `ContentControl`/`Border` placeholders for
  *toolbar*, *left rail*, *viewport*, *inspector*, *status bar*. Each region binds to a ViewModel property.
  Wave-2 tracks fill these with self-contained `UserControl`s — they never edit `MainWindow.xaml`.
- **ViewModel contracts (interfaces + base shells):** `IMainViewModel`, `IToolRailViewModel`,
  `IInspectorViewModel`, `IStatusBarViewModel`, `IViewportViewModel`. Properties/commands named now; bodies
  filled by owning tracks.
- **Service contracts:** `ISceneService` (current `SceneObject`, `LoadAsync`, `ExportAsync`, `Undo`, `Redo`,
  `Reset`, `ProposalPreview`), `IPickingService` (raise `DatumPicked`), `IDialogService`, `IUnitService`.
- **`IViewportController`** abstraction so ViewModels talk to the viewport without referencing HelixToolkit.
- **Theme:** one `Themes/Dark.xaml` resource dictionary (colors, brushes, control styles, the teal accent),
  merged once in `App.xaml`. Panels reference keys only.
- **Empty `HelixViewportHost` UserControl** (renders nothing yet) placed in the viewport region.

**DoD:** app launches to the dark four-zone shell with empty regions and an empty 3D host; DI resolves all
service interfaces (to stubs); no track needs to touch `MainWindow.xaml`/`App.xaml`/`Dark.xaml` again.

---

## 5. WAVE 2 & 3 — feature tracks

### WAVE 2 (after Wave 1)

**T6 — Plane alignment tools · `Core/Alignment/ThreePointPlaneTool.cs, BestFitPlaneTool.cs`**
Implement `IAlignmentTool` for 3-point and best-fit-plane alignment using `IPlaneFitter` (T4). Map fitted
plane → target world plane + axis; apply origin policy. **DoD:** tilted-plane fixture aligns flat to XY within
tolerance; proposal residual = fit RMS; auto-discovered by registry.

**T7 — Line/Origin/PCA alignment tools · `Core/Alignment/TwoPointLineTool.cs, PointToOriginTool.cs, PcaAutoTool.cs`**
2-point/2-hole line→axis (uses `ILineFitter`/`ICircleFitter`), point→origin (translation only), PCA auto
(uses `IPcaAligner`). **DoD:** two-hole fixture aligns the hole-line to X; point→origin lands point at (0,0,0);
PCA pre-aligns the skewed cloud; all three discovered by registry.

**T8 — Measurement tools · `Core/Measure/Measurements.cs`**
Implement `IMeasurements` (distance+components, angle, diameter via T5 circle fit, bbox). **DoD:** results match
fixture ground truth.

**T10 — Viewport rendering · `App/Viewport/Rendering/*`**
HelixToolkit.SharpDX scene: render `MeshData` (ghost original + solid preview), axis triad, ground grid,
camera/orbit, standard views, frame-selection. Implements `IViewportController` render side. **DoD:** loads a
fixture mesh via `ISceneService` and displays it with triad+grid; handles a 1M-triangle mesh smoothly.

**T12 — Panels · `App/Views/Panels/*` (+ matching ViewModels)**
Three self-contained `UserControl`s filling their regions: **ToolRail** (lists `AlignmentToolRegistry.Tools`,
step indicator), **Inspector** (target "Snap to" control, residual readout, transform tx/ty/tz·rx/ry/rz, commit
button, alignment-stack history), **StatusBar** (coordinate probe, bbox, triangle count, units). **DoD:** rail
lists discovered tools; inspector binds to a sample proposal; status bar shows live values from a stub scene.

### WAVE 3 (after Wave 2)

**T11 — Viewport interaction · `App/Viewport/Interaction/*`**
Ray-cast picking (g3 spatial index), snap-to-vertex, numbered datum markers, brush-select for best-fit, the
manual nudge gizmo (drag + typed exact values). Raises `IPickingService.DatumPicked`. **DoD:** clicking the mesh
produces correct world-space datums; gizmo edits the live transform; markers render and are draggable.

**T14 — Scene orchestration wiring · `App/Services/SceneService.cs`, `App/ViewModels/MainViewModel.cs`**
The conductor: implement `ISceneService` + `MainViewModel` to run the loop
**load → select tool → collect picks → solve (registry) → live preview → commit → undo/redo/reset → export**.
Wires viewport, panels, picking, registries, transform stack together. **DoD:** full loop works headlessly in a
ViewModel-level test; preview updates on each pick; commit pushes an `AlignmentStep`.

---

## 6. File ownership map (the collision-free contract)

Each path prefix is owned by exactly one track. Agents stay inside their prefix.

| Track | Owns (create/edit only here) |
|---|---|
| **Wave 0** | `ScanAlign.sln`, `Directory.*.props`, `build.ps1`, CI, `Core/Model/**`, `Core/IO/I*Reader/Writer/Detector` interfaces, `Core/Solvers/I*` interfaces, `Core/Alignment/IAlignmentTool.cs`, `Core/Measure/IMeasurements.cs`+result records, `Core/Registry/**`, `Tests/Fixtures/**`, `Tests/Smoke*` |
| **T1 OBJ** | `Core/IO/Obj*.cs`, `Tests/IO/Obj*Tests.cs` |
| **T2 PLY** | `Core/IO/Ply*.cs`, `Tests/IO/Ply*Tests.cs` |
| **T3 STL** | `Core/IO/Stl*.cs`, `Tests/IO/Stl*Tests.cs` |
| **T4 solvers** | `Core/Solvers/PlaneFitter.cs`, `Core/Solvers/LineFitter.cs`, `Tests/Solvers/Plane*,Line*Tests.cs` |
| **T5 solvers** | `Core/Solvers/CircleFitter.cs`, `PcaAligner.cs`, `BoundingBox.cs`, `Tests/Solvers/Circle*,Pca*,Bbox*Tests.cs` |
| **T13 units** | `Core/IO/UnitDetector.cs`, `Tests/IO/UnitDetectorTests.cs` |
| **B App-found.** | `App/App.xaml(.cs)`, `App/MainWindow.xaml(.cs)`, `App/Themes/**`, `App/Abstractions/**` (VM+service interfaces), `App/Composition/**`, `App/Viewport/HelixViewportHost.*` |
| **T6 align** | `Core/Alignment/ThreePointPlaneTool.cs`, `BestFitPlaneTool.cs`, `Tests/Alignment/Plane*Tests.cs` |
| **T7 align** | `Core/Alignment/TwoPointLineTool.cs`, `PointToOriginTool.cs`, `PcaAutoTool.cs`, `Tests/Alignment/Line*,Origin*,Pca*Tests.cs` |
| **T8 measure** | `Core/Measure/Measurements.cs`, `Tests/Measure/**` |
| **T10 render** | `App/Viewport/Rendering/**` |
| **T12 panels** | `App/Views/Panels/**`, `App/ViewModels/ToolRail*,Inspector*,StatusBar*.cs` |
| **T11 interact** | `App/Viewport/Interaction/**`, `App/Services/PickingService.cs` |
| **T14 orchestr.** | `App/Services/SceneService.cs`, `App/Services/UnitService.cs`, `App/ViewModels/MainViewModel.cs`, `App/ViewModels/ViewportViewModel.cs` |
| **Wave 4** | integration glue, `installer/**`, `docs/**`, perf — touches seams across tracks *after* all merge |

No two rows share a file. Tests are SDK-globbed, so adding test files never edits a `.csproj`.

---

## 7. WAVE 4 — Integration & ship (1–2 agents, after all merge)

- End-to-end glue & seam-fixing across tracks; run the real 60-second loop on your actual scanner exports.
- **MSIX installer** (or single-file self-contained publish) + app icon + first-run experience.
- **Performance pass:** display-only decimation for huge meshes, streaming progress bars, picking index tuning.
- Provenance header on export; "reset to imported" verified; keyboard shortcuts (§9 of PLAN.md).
- Short user README + the §13 "drag OBJ → 3-point-plane → Z → export" walkthrough as a manual test script.

---

## 8. How to actually run the agents

1. **Now:** `git init`; one agent runs **Wave 0** to completion; review + merge to `main`.
2. **Wave 1:** create worktrees `track/obj`, `track/ply`, `track/stl`, `track/solvers-a`, `track/solvers-b`,
   `track/units`, `track/app-foundation`. Launch up to 7 agents, one per branch. Merge each as it goes green.
3. **Wave 2:** branch `track/align-plane`, `track/align-line`, `track/measure`, `track/render`, `track/panels`.
   Up to 5 agents. Merge green branches.
4. **Wave 3:** `track/interaction`, `track/orchestration`. 2 agents. Merge.
5. **Wave 4:** 1–2 agents integrate and ship.

**Each agent's brief is identical in shape:** "Implement track _T#_ per IMPLEMENTATION.md §_x_. Touch only the
files in your §6 ownership row. Code against the frozen Core contracts — do not change any contract signature.
Write tests in your owned `Tests/` paths. Done = `dotnet test` green and no `NotImplementedException` left in
your scope."

---

*Wave 0 is the only true bottleneck. Get the contracts right and the rest fans out cleanly.*
