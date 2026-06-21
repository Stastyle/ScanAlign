using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Alignment;

public class CentroidAndPlacementTests
{
    private static Datum Centroid(IReadOnlyList<Vector3> pts)
    {
        var sum = Vector3.Zero;
        foreach (var p in pts)
        {
            sum += p;
        }

        return new Datum(DatumKind.Centroid, sum / pts.Count, SupportPoints: pts);
    }

    [Fact]
    public void Centroid_of_rim_points_approximates_circle_center_and_moves_to_origin()
    {
        var plate = SyntheticGeometry.TwoHolePlate(radius: 3f, separation: 16f);
        var datum = Centroid(plate.RimA);

        // The average of evenly spaced rim points lands on the true center.
        Assert.True(Vector3.Distance(datum.Position, plate.CenterA) < 1e-3f);

        var proposal = new CentroidOriginTool().Solve(new[] { datum }, AlignmentTarget.Default);
        Assert.True(Vector3.Transform(datum.Position, proposal.Transform).Length() < 1e-4f);
    }

    [Fact]
    public void Center_to_center_line_aligns_to_axis()
    {
        var plate = SyntheticGeometry.TwoHolePlate(separation: 20f); // centers along X
        var picks = new[] { Centroid(plate.RimA), Centroid(plate.RimB) };
        var target = new AlignmentTarget(TargetKind.AxisY, OriginPolicy.Keep, UpAxis.Z);

        var proposal = new CentroidLineTool().Solve(picks, target);

        var a = Vector3.Transform(picks[0].Position, proposal.Transform);
        var b = Vector3.Transform(picks[1].Position, proposal.Transform);
        var dir = Vector3.Normalize(b - a);
        Assert.True(MathF.Abs(dir.Y) > 0.999f, $"should align to Y, got {dir}");
    }

    [Fact]
    public void Line_on_axis_places_points_on_the_axis()
    {
        var a = new Vector3(2, 5, 7);
        var b = new Vector3(2, 5, 12); // along Z, off the X axis
        var picks = new[] { new Datum(DatumKind.Point, a), new Datum(DatumKind.Point, b) };
        var target = new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z, Placement: AxisPlacement.OnAxis);

        var p = new TwoPointLineTool().Solve(picks, target);

        var ta = Vector3.Transform(a, p.Transform);
        var tb = Vector3.Transform(b, p.Transform);
        Assert.True(MathF.Abs(ta.Y) < 1e-3f && MathF.Abs(ta.Z) < 1e-3f, $"a not on X axis: {ta}");
        Assert.True(MathF.Abs(tb.Y) < 1e-3f && MathF.Abs(tb.Z) < 1e-3f, $"b not on X axis: {tb}");
    }

    [Fact]
    public void Line_parallel_keeps_position_offset()
    {
        var a = new Vector3(2, 5, 7);
        var b = new Vector3(2, 5, 12);
        var picks = new[] { new Datum(DatumKind.Point, a), new Datum(DatumKind.Point, b) };
        var target = new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z, Placement: AxisPlacement.Parallel);

        var p = new TwoPointLineTool().Solve(picks, target);

        var ta = Vector3.Transform(a, p.Transform);
        Assert.True(Vector3.Distance(ta, a) < 1e-3f, "parallel+keep should leave the anchor in place");
    }

    [Fact]
    public void Plane_on_axis_lies_in_target_plane_even_when_keeping_position()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 20, n: 7);
        // Three non-collinear points (corners of the grid), so the plane is well-defined.
        var picks = new[] { sample.Points[0], sample.Points[6], sample.Points[^1] }
            .Select(pt => new Datum(DatumKind.Point, pt)).ToArray();

        var coincident = new ThreePointPlaneTool().Solve(picks,
            new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.Keep, UpAxis.Z, Placement: AxisPlacement.OnAxis));
        var parallel = new ThreePointPlaneTool().Solve(picks,
            new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.Keep, UpAxis.Z, Placement: AxisPlacement.Parallel));

        var zc = sample.Points.Select(pt => Vector3.Transform(pt, coincident.Transform).Z).ToArray();
        var zp = sample.Points.Select(pt => Vector3.Transform(pt, parallel.Transform).Z).ToArray();

        Assert.True(zc.Max() - zc.Min() < 1e-3f && MathF.Abs(zc[0]) < 1e-3f, "on-axis face should lie in z=0");
        Assert.True(zp.Max() - zp.Min() < 1e-3f && MathF.Abs(zp[0]) > 0.1f, "parallel face should keep its height offset");
    }
}
