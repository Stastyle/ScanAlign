using System.Windows.Media.Media3D;

namespace ScanAlign.App.Viewport;

/// <summary>
/// Builds the ground grid and axis triad. Native WPF 3D has no line primitive, so each line is a
/// thin square-section bar. Sizes adapt to the loaded object's extent.
/// </summary>
internal static class GridBuilder
{
    public static MeshGeometry3D BuildGrid(float extent, int divisions)
    {
        var geo = new MeshGeometry3D();
        var step = extent / divisions;
        var radius = step * 0.01f;
        for (var i = -divisions; i <= divisions; i++)
        {
            var t = i * step;
            AddBar(geo, new Vector3(-extent, t, 0), new Vector3(extent, t, 0), radius);
            AddBar(geo, new Vector3(t, -extent, 0), new Vector3(t, extent, 0), radius);
        }

        return geo;
    }

    public static MeshGeometry3D BuildAxis(Vector3 direction, float length)
    {
        var geo = new MeshGeometry3D();
        AddBar(geo, Vector3.Zero, direction * length, length * 0.012f);
        return geo;
    }

    public static void AddBar(MeshGeometry3D geo, Vector3 a, Vector3 b, float radius)
    {
        var dir = b - a;
        if (dir.LengthSquared() < 1e-20f)
        {
            return;
        }

        dir = Vector3.Normalize(dir);
        var up = MathF.Abs(dir.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX;
        var right = Vector3.Normalize(Vector3.Cross(dir, up)) * radius;
        var top = Vector3.Normalize(Vector3.Cross(right, dir)) * radius;

        var baseIndex = geo.Positions.Count;
        Span<Vector3> corners =
        [
            a + right + top, a - right + top, a - right - top, a + right - top,
            b + right + top, b - right + top, b - right - top, b + right - top,
        ];
        foreach (var c in corners)
        {
            geo.Positions.Add(new Point3D(c.X, c.Y, c.Z));
        }

        ReadOnlySpan<int> sides =
        [
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7,
        ];
        foreach (var s in sides)
        {
            geo.TriangleIndices.Add(baseIndex + s);
        }
    }
}
