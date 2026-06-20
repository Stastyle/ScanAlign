using ScanAlign.Core.Solvers;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.Solvers;

public class PlaneFitterTests
{
    private readonly PlaneFitter _fitter = new();

    [Fact]
    public void Three_points_define_an_exact_plane()
    {
        var p0 = new Vector3(0, 0, 0);
        var p1 = new Vector3(1, 0, 0);
        var p2 = new Vector3(0, 1, 0);

        var result = _fitter.Fit(new[] { p0, p1, p2 });

        Assert.Equal(0.0, result.Rms, 6);
        Assert.Equal(1f, MathF.Abs(Vector3.Dot(result.Plane.Normal, Vector3.UnitZ)), 5);
    }

    [Fact]
    public void Best_fit_recovers_a_tilted_plane_normal_with_near_zero_rms()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 25);

        var result = _fitter.Fit(sample.Points);

        // Normal is recovered up to sign.
        Assert.Equal(1f, MathF.Abs(Vector3.Dot(result.Plane.Normal, sample.Normal)), 4);
        Assert.True(result.Rms < 1e-4, $"RMS {result.Rms} should be ~0 for points exactly on a plane");
    }

    [Fact]
    public void Rms_reports_deviation_for_noisy_points()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 0); // flat z=const grid
        var pts = sample.Points.ToList();
        pts.Add(sample.PointOnPlane + (sample.Normal * 0.5f)); // one point 0.5 off the plane

        var result = _fitter.Fit(pts);

        Assert.True(result.Rms > 0.01, "RMS should grow when a point lies off the plane");
    }

    [Fact]
    public void Collinear_three_points_throw()
    {
        var pts = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0) };
        Assert.Throws<ArgumentException>(() => _fitter.Fit(pts));
    }
}
