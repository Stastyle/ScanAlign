using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;

namespace ScanAlign.Tests.Alignment;

public class ThreeTwoOneToolTests
{
    [Fact]
    public void Fully_constrains_a_part_from_six_points()
    {
        // A known local frame: primary plane on local z=0, secondary edge along local +X.
        var local = new[]
        {
            new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(0, 8, 0), // primary plane
            new Vector3(2, 4, 0), new Vector3(9, 4, 0),                          // secondary line (+X)
            new Vector3(3, 3, 0),                                                // tertiary
        };

        // Scatter it into world space with an arbitrary rigid pose.
        var pose = Matrix4x4.CreateRotationX(0.5f) * Matrix4x4.CreateRotationY(0.4f) *
                   Matrix4x4.CreateRotationZ(0.3f) * Matrix4x4.CreateTranslation(50, -20, 30);
        var picks = local.Select(p => new Datum(DatumKind.Point, Vector3.Transform(p, pose))).ToArray();

        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z);
        var proposal = new ThreeTwoOneTool().Solve(picks, target);

        Assert.True(proposal.IsComplete);
        var m = proposal.Transform;

        // Primary plane becomes parallel to XY (near-constant Z).
        var planeZ = picks.Take(3).Select(d => Vector3.Transform(d.Position, m).Z).ToArray();
        Assert.True(planeZ.Max() - planeZ.Min() < 1e-3f);

        // The tertiary point (index 5) becomes the origin.
        Assert.True(Vector3.Transform(picks[5].Position, m).Length() < 1e-4f);

        // Secondary edge aligns to +X.
        var d0 = Vector3.Transform(picks[3].Position, m);
        var d1 = Vector3.Transform(picks[4].Position, m);
        var dir = Vector3.Normalize(d1 - d0);
        Assert.True(MathF.Abs(dir.X) > 0.999f, $"edge should align to X, got {dir}");
        Assert.True(MathF.Abs(dir.Y) < 1e-3f && MathF.Abs(dir.Z) < 1e-3f);
    }

    [Fact]
    public void Secondary_edge_aligns_to_the_chosen_axis()
    {
        var local = new[]
        {
            new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(0, 8, 0),
            new Vector3(2, 4, 0), new Vector3(9, 4, 0),   // edge along local +X
            new Vector3(3, 3, 0),
        };
        var pose = Matrix4x4.CreateRotationY(0.5f) * Matrix4x4.CreateTranslation(10, 20, 30);
        var picks = local.Select(p => new Datum(DatumKind.Point, Vector3.Transform(p, pose))).ToArray();

        // Primary face → XY, but route the secondary edge to Y instead of the default X.
        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z, Secondary: TargetKind.AxisY);
        var m = new ThreeTwoOneTool().Solve(picks, target).Transform;

        var edge = Vector3.Normalize(Vector3.Transform(picks[4].Position, m) - Vector3.Transform(picks[3].Position, m));
        Assert.True(MathF.Abs(edge.Y) > 0.999f, $"edge should align to Y, got {edge}");
    }

    [Fact]
    public void Reports_staged_prompts_while_collecting()
    {
        var tool = new ThreeTwoOneTool();
        Assert.Contains("Primary", tool.Solve(Array.Empty<Datum>(), AlignmentTarget.Default).Explanation);

        var three = Enumerable.Range(0, 3).Select(i => new Datum(DatumKind.Point, new Vector3(i, 0, 0))).ToArray();
        Assert.Contains("Secondary", tool.Solve(three, AlignmentTarget.Default).Explanation);
    }
}
