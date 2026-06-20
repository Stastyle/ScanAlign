using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Alignment;

public class BestFitPlanePointsTests
{
    [Fact]
    public void Fits_from_many_individually_clicked_points()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 17, n: 7);
        // Simulate the viewport adding one Point datum per click (no SupportPoints region).
        var picks = sample.Points.Select(p => new Datum(DatumKind.Point, p)).ToArray();
        var target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z);

        var proposal = new BestFitPlaneTool().Solve(picks, target);

        Assert.True(proposal.IsComplete);
        var zs = sample.Points.Select(p => Vector3.Transform(p, proposal.Transform).Z).ToArray();
        Assert.True(zs.Max() - zs.Min() < 1e-3f);
    }

    [Fact]
    public void Pending_below_three_points()
    {
        var picks = new[] { new Datum(DatumKind.Point, Vector3.Zero), new Datum(DatumKind.Point, Vector3.UnitX) };
        Assert.False(new BestFitPlaneTool().Solve(picks, AlignmentTarget.Default).IsComplete);
    }
}
