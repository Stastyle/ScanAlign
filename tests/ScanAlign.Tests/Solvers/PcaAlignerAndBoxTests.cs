using ScanAlign.Core.Solvers;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Solvers;

public class PcaAlignerAndBoxTests
{
    [Fact]
    public void Pca_align_maps_primary_axis_to_world_x()
    {
        // A larger sample makes the sample principal axis converge tightly to the true X axis.
        var cloud = SyntheticGeometry.SkewedCloud(n: 4000, seed: 7);
        var m = new PcaAligner().PrincipalAxesToWorld(cloud.Points);

        var mapped = Vector3.Normalize(Vector3.TransformNormal(cloud.PrimaryAxis, m));
        Assert.True(MathF.Abs(mapped.X) > 0.99f, $"primary should map onto X, got {mapped}");
        Assert.True(MathF.Abs(mapped.Y) < 0.1f && MathF.Abs(mapped.Z) < 0.1f, $"off-axis leakage too large: {mapped}");
    }

    [Fact]
    public void Pca_align_is_a_proper_rotation()
    {
        var cloud = SyntheticGeometry.SkewedCloud();
        var m = new PcaAligner().PrincipalAxesToWorld(cloud.Points);

        // Proper rotation: determinant +1 and orthonormal.
        Assert.Equal(1f, m.GetDeterminant(), 3);
    }

    [Fact]
    public void BoundingBox_matches_known_extents()
    {
        var pts = new[]
        {
            new Vector3(-1, -2, -3),
            new Vector3(4, 5, 6),
            new Vector3(0, 1, 0),
        };

        var box = new BoundingBox().Compute(pts);

        Assert.Equal(new Vector3(-1, -2, -3), box.Min);
        Assert.Equal(new Vector3(4, 5, 6), box.Max);
        Assert.Equal(new Vector3(5, 7, 9), box.Size);
    }
}
