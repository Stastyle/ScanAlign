# Known issues — ScanAlign Alpha v0.1

This is an early alpha. The geometry/alignment core is well tested, but the interactive app has
rough edges. Known limitations and bugs, roughly by priority:

## Picking & interaction

- **Picking precision needs more work.** Clicking on dense or noisy meshes can land a datum slightly
  off the intended spot, and occasionally a click near a silhouette edge misses the surface. More
  debugging is planned.
- **No snap-to-vertex or snap-to-feature.** Picks land on the ray-hit surface point, not the nearest
  mesh vertex or detected feature.
- **No manual nudge gizmo yet.** Fine orientation/position tweaks after an alignment aren't available
  in the UI (you can re-pick or use Flip / Place-on-axis instead).
- **Best-fit plane is click-by-click, not a drag-brush.** You add region points one click at a time.

## Viewport & performance

- **Native WPF 3D backend.** The viewport uses Windows' built-in 3D (no GPU/DirectX backend yet), so
  very large meshes (multi-million triangles) may render and pick slowly. Point clouds are
  down-sampled for display above ~40k points. A GPU backend can be added behind `IViewportController`.
- **Axis labels are a corner legend**, not 3D labels at the axis ends.

## Alignment behavior

- **3-2-1 origin** is placed at the *tertiary point you pick*, not the strict intersection of the
  three datum planes. This is intuitive but not full GD&T datum-frame construction.
- **Center tools** use a least-squares **circle** fit. This is ideal for round holes and good for
  symmetric pockets, but a non-circular feature sampled unevenly can still bias the center — sample
  around the whole boundary for best results.
- **`Center bounding box` origin policy** is treated like a centered move for most tools; it is not a
  full per-mesh bounding-box recentering for every tool.

## Not yet in the UI

- **Measurement tools** (distance, hole diameter, angle, bounding box) exist in `ScanAlign.Core` and
  are tested, but are not yet surfaced in the app UI beyond the status-bar bounding box.
- **Unit handling.** The unit is detected heuristically and shown, but there is no "convert/rescale
  units" action on import or export yet.

## Packaging

- **No installer yet.** Run from source via `dotnet run` (see the README). An MSIX installer and app
  icon are planned.

---

Found something not listed here? Please open an issue on GitHub.
