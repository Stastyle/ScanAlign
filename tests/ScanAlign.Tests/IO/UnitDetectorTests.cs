using ScanAlign.Core.IO;
using ScanAlign.Core.Model;

namespace ScanAlign.Tests.IO;

public class UnitDetectorTests
{
    private readonly UnitDetector _detector = new();

    private static MeshData CubeOfSize(float side) => new(
        new[] { Vector3.Zero, new Vector3(side, side, side) },
        Array.Empty<int>(), null, null, Unit.Unknown, null);

    [Theory]
    [InlineData(100f, Unit.Millimeter)]  // 100 -> 100 mm object
    [InlineData(0.12f, Unit.Meter)]      // 0.12 -> 120 mm object
    [InlineData(6f, Unit.Inch)]          // 6 -> 152 mm object (6 inches)
    public void Suggests_plausible_unit(float side, Unit expected)
    {
        Assert.Equal(expected, _detector.Detect(CubeOfSize(side)));
    }

    [Fact]
    public void Returns_unknown_for_empty_mesh()
    {
        Assert.Equal(Unit.Unknown, _detector.Detect(MeshData.PointCloud(Array.Empty<Vector3>())));
    }

    [Fact]
    public void Returns_unknown_for_implausible_scale()
    {
        // 10 million units across is not a plausible physical object in any supported unit.
        Assert.Equal(Unit.Unknown, _detector.Detect(CubeOfSize(1e7f)));
    }
}
