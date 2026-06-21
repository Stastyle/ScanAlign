using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;

namespace ScanAlign.Tests.Alignment;

public class FlipTests
{
    private static (Vector3 mappedNormal, Vector3 planeNormal) AlignFlat(bool flip)
    {
        // A flat face whose outward normal is +Z in world.
        var picks = new[]
        {
            new Datum(DatumKind.Point, new Vector3(0, 0, 0)),
            new Datum(DatumKind.Point, new Vector3(4, 0, 0)),
            new Datum(DatumKind.Point, new Vector3(0, 3, 0)),
        };
        var planeNormal = Vector3.Normalize(Vector3.Cross(
            picks[1].Position - picks[0].Position, picks[2].Position - picks[0].Position));

        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z, Flip: flip);
        var proposal = new ThreePointPlaneTool().Solve(picks, target);
        var mapped = Vector3.Normalize(Vector3.TransformNormal(planeNormal, proposal.Transform));
        return (mapped, planeNormal);
    }

    [Fact]
    public void Without_flip_normal_points_up()
    {
        var (mapped, _) = AlignFlat(flip: false);
        Assert.True(mapped.Z > 0.999f, $"expected +Z, got {mapped}");
    }

    [Fact]
    public void With_flip_normal_points_down()
    {
        var (mapped, _) = AlignFlat(flip: true);
        Assert.True(mapped.Z < -0.999f, $"expected -Z, got {mapped}");
    }
}
