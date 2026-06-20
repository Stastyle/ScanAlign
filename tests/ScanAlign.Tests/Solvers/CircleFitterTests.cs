using ScanAlign.Core.Solvers;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Solvers;

public class CircleFitterTests
{
    private readonly CircleFitter _fitter = new();

    [Fact]
    public void Recovers_hole_center_radius_and_normal()
    {
        var plate = SyntheticGeometry.TwoHolePlate(radius: 2.5f, separation: 20f);

        var result = _fitter.Fit(plate.RimA);

        Assert.True(Vector3.Distance(result.Circle.Center, plate.CenterA) < 1e-3,
            $"center {result.Circle.Center} vs {plate.CenterA}");
        Assert.Equal(plate.Radius, result.Circle.Radius, 3);
        Assert.Equal(1f, MathF.Abs(Vector3.Dot(result.Circle.Normal, plate.Normal)), 4);
        Assert.True(result.Rms < 1e-3);
    }

    [Fact]
    public void Fits_a_tilted_circle()
    {
        // Circle of radius 4 centered at (1,2,3), tilted into an arbitrary plane.
        var n = Vector3.Normalize(new Vector3(1, 2, 2));
        var u = Vector3.Normalize(Vector3.Cross(n, Vector3.UnitX));
        var v = Vector3.Cross(n, u);
        var center = new Vector3(1, 2, 3);
        var pts = new List<Vector3>();
        for (var i = 0; i < 36; i++)
        {
            var a = i / 36f * MathF.Tau;
            pts.Add(center + (u * (MathF.Cos(a) * 4f)) + (v * (MathF.Sin(a) * 4f)));
        }

        var result = _fitter.Fit(pts);

        Assert.True(Vector3.Distance(result.Circle.Center, center) < 1e-2);
        Assert.Equal(4f, result.Circle.Radius, 2);
    }
}
