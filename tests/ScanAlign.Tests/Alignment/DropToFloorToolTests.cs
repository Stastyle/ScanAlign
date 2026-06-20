using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;

namespace ScanAlign.Tests.Alignment;

public class DropToFloorToolTests
{
    [Fact]
    public void Rests_lowest_point_on_z_zero()
    {
        var cloud = new[]
        {
            new Vector3(1, 2, 5),
            new Vector3(3, 4, -2.5f), // lowest
            new Vector3(0, 0, 8),
        };
        var datum = new[] { new Datum(DatumKind.PlaneRegion, Vector3.Zero, SupportPoints: cloud) };

        var proposal = new DropToFloorTool().Solve(datum, AlignmentTarget.Default);

        Assert.True(proposal.IsComplete);
        var moved = cloud.Select(p => Vector3.Transform(p, proposal.Transform)).ToArray();
        Assert.Equal(0f, moved.Min(p => p.Z), 4);
        // X and Y are untouched.
        Assert.Equal(1f, moved[0].X, 4);
        Assert.Equal(2f, moved[0].Y, 4);
    }

    [Fact]
    public void Pending_without_a_cloud()
    {
        var proposal = new DropToFloorTool().Solve(Array.Empty<Datum>(), AlignmentTarget.Default);
        Assert.False(proposal.IsComplete);
    }
}
