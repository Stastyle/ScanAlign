using ScanAlign.Core.Solvers;

namespace ScanAlign.Tests.Solvers;

public class LineFitterTests
{
    private readonly LineFitter _fitter = new();

    [Fact]
    public void Two_points_define_an_exact_line()
    {
        var result = _fitter.Fit(new[] { new Vector3(1, 2, 3), new Vector3(1, 2, 8) });

        Assert.Equal(0.0, result.Rms, 6);
        Assert.Equal(1f, MathF.Abs(Vector3.Dot(result.Line.Direction, Vector3.UnitZ)), 5);
    }

    [Fact]
    public void Best_fit_recovers_a_known_axis_direction()
    {
        var dir = Vector3.Normalize(new Vector3(1, 1, 0));
        var origin = new Vector3(-2, 3, 1);
        var pts = new List<Vector3>();
        for (var i = -10; i <= 10; i++)
        {
            pts.Add(origin + (dir * (i * 0.7f)));
        }

        var result = _fitter.Fit(pts);

        Assert.Equal(1f, MathF.Abs(Vector3.Dot(result.Line.Direction, dir)), 4);
        Assert.True(result.Rms < 1e-4);
    }

    [Fact]
    public void Perpendicular_spread_raises_rms()
    {
        var pts = new List<Vector3>();
        for (var i = -5; i <= 5; i++)
        {
            pts.Add(new Vector3(i, (i % 2) * 0.3f, 0)); // zig-zag around the X axis
        }

        var result = _fitter.Fit(pts);

        Assert.True(result.Rms > 0.05);
    }
}
