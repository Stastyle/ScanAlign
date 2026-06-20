using ScanAlign.Core.Measure;
using ScanAlign.Core.Model;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Measure;

public class MeasurementsTests
{
    private readonly Measurements _m = new();

    [Fact]
    public void Distance_returns_length_and_components()
    {
        var r = _m.Distance(new Vector3(1, 1, 0), new Vector3(4, 5, 0));
        Assert.Equal(5f, r.Distance, 4);
        Assert.Equal(new Vector3(3, 4, 0), r.Delta);
    }

    [Fact]
    public void Angle_perpendicular_axes_is_90()
    {
        var r = _m.Angle(new Line3(Vector3.Zero, Vector3.UnitX), new Line3(Vector3.Zero, Vector3.UnitY));
        Assert.Equal(90f, r.Degrees, 3);
    }

    [Fact]
    public void Angle_parallel_axes_is_0()
    {
        var r = _m.Angle(new Line3(Vector3.Zero, Vector3.UnitX), new Line3(Vector3.Zero, Vector3.UnitX));
        Assert.Equal(0f, r.Degrees, 3);
    }

    [Fact]
    public void Angle_opposite_directions_is_180()
    {
        var r = _m.Angle(new Line3(Vector3.Zero, Vector3.UnitX), new Line3(Vector3.Zero, -Vector3.UnitX));
        Assert.Equal(180f, r.Degrees, 3);
    }

    [Fact]
    public void Diameter_recovers_hole_size()
    {
        var plate = SyntheticGeometry.TwoHolePlate(radius: 3f);
        var r = _m.Diameter(plate.RimA);
        Assert.Equal(6f, r.Diameter, 2);
        Assert.True(r.Rms < 1e-3);
    }

    [Fact]
    public void BoundingBox_returns_extents()
    {
        var box = _m.BoundingBox(new[] { new Vector3(-1, 0, 2), new Vector3(3, 5, -4) });
        Assert.Equal(new Vector3(-1, 0, -4), box.Min);
        Assert.Equal(new Vector3(3, 5, 2), box.Max);
    }
}
