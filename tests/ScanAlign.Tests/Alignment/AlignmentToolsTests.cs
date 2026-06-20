using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Alignment;

public class AlignmentToolsTests
{
    private static float ZSpread(IEnumerable<Vector3> pts, Matrix4x4 m)
    {
        var zs = pts.Select(p => Vector3.Transform(p, m).Z).ToArray();
        return zs.Max() - zs.Min();
    }

    [Fact]
    public void ThreePointPlane_makes_a_tilted_face_parallel_to_xy_at_origin()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 22, n: 11);
        var p = sample.Points;
        var picks = new[]
        {
            new Datum(DatumKind.Point, p[0]),
            new Datum(DatumKind.Point, p[10]),
            new Datum(DatumKind.Point, p[^1]),
        };
        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z);

        var proposal = new ThreePointPlaneTool().Solve(picks, target);

        Assert.True(proposal.IsComplete);
        // Every point of the (planar) face should collapse to a near-constant Z (parallel to XY).
        Assert.True(ZSpread(p, proposal.Transform) < 1e-3f, "face should be parallel to XY after alignment");
        // The anchor (first pick) should land on the origin plane.
        Assert.True(MathF.Abs(Vector3.Transform(p[0], proposal.Transform).Z) < 1e-4f);
    }

    [Fact]
    public void ThreePointPlane_is_pending_with_too_few_picks()
    {
        var proposal = new ThreePointPlaneTool().Solve(
            new[] { new Datum(DatumKind.Point, Vector3.Zero) }, AlignmentTarget.Default);
        Assert.False(proposal.IsComplete);
        Assert.Equal(Matrix4x4.Identity, proposal.Transform);
    }

    [Fact]
    public void BestFitPlane_uses_region_support_points()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 15, n: 9);
        var region = new[] { new Datum(DatumKind.PlaneRegion, sample.PointOnPlane, SupportPoints: sample.Points) };
        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z);

        var proposal = new BestFitPlaneTool().Solve(region, target);

        Assert.True(proposal.IsComplete);
        Assert.True(ZSpread(sample.Points, proposal.Transform) < 1e-3f);
    }

    [Fact]
    public void TwoPointLine_aligns_the_line_to_the_x_axis()
    {
        var a = new Vector3(2, 3, 4);
        var b = new Vector3(5, 7, 1); // arbitrary direction
        var picks = new[] { new Datum(DatumKind.Point, a), new Datum(DatumKind.Point, b) };
        var target = new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z);

        var proposal = new TwoPointLineTool().Solve(picks, target);

        var ta = Vector3.Transform(a, proposal.Transform);
        var tb = Vector3.Transform(b, proposal.Transform);
        var dir = Vector3.Normalize(tb - ta);
        Assert.Equal(1f, MathF.Abs(dir.X), 4);                 // aligned to X
        Assert.True(MathF.Abs(dir.Y) < 1e-3f && MathF.Abs(dir.Z) < 1e-3f);
        // Keep policy anchors the first datum.
        Assert.True(Vector3.Distance(ta, a) < 1e-3f);
    }

    [Fact]
    public void PointToOrigin_moves_the_pick_to_zero()
    {
        var pick = new Vector3(-4, 8, 2);
        var proposal = new PointToOriginTool().Solve(new[] { new Datum(DatumKind.Point, pick) }, AlignmentTarget.Default);

        Assert.True(Vector3.Transform(pick, proposal.Transform).Length() < 1e-5f);
    }

    [Fact]
    public void PcaAuto_aligns_primary_axis_to_x()
    {
        var cloud = SyntheticGeometry.SkewedCloud(n: 4000, seed: 11);
        var datum = new[] { new Datum(DatumKind.PlaneRegion, Vector3.Zero, SupportPoints: cloud.Points) };

        var proposal = new PcaAutoTool().Solve(datum, new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z));

        var mapped = Vector3.Normalize(Vector3.TransformNormal(cloud.PrimaryAxis, proposal.Transform));
        Assert.True(MathF.Abs(mapped.X) > 0.99f, $"primary axis should map to X, got {mapped}");
    }

    [Fact]
    public void Registry_discovers_all_five_tools()
    {
        var ids = new AlignmentToolRegistry().Tools.Select(t => t.Id).ToHashSet();

        Assert.Superset(
            new HashSet<string> { "three-point-plane", "best-fit-plane", "two-point-line", "point-to-origin", "pca-auto" },
            ids);
    }
}
