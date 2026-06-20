using ScanAlign.Core.Model;

namespace ScanAlign.Tests.Support;

/// <summary>
/// Deterministic golden geometry with known ground truth. Shared, read-only test infrastructure
/// for the solver / alignment / measurement tracks. Tracks CALL these helpers — they do not edit
/// this file (it is owned by Wave 0).
/// </summary>
public static class SyntheticGeometry
{
    /// <summary>A grid of points sampled exactly on a tilted plane, with the plane's ground truth.</summary>
    public sealed record PlaneSample(IReadOnlyList<Vector3> Points, Vector3 Normal, Vector3 PointOnPlane);

    /// <summary>Two circle rims in a shared plane, with each center, radius, and plane normal.</summary>
    public sealed record TwoHoleSample(
        IReadOnlyList<Vector3> RimA,
        IReadOnlyList<Vector3> RimB,
        Vector3 CenterA,
        Vector3 CenterB,
        float Radius,
        Vector3 Normal);

    /// <summary>A point cloud whose principal axes are known (for PCA / bbox checks).</summary>
    public sealed record CloudSample(IReadOnlyList<Vector3> Points, Vector3 PrimaryAxis, Aabb Box);

    /// <summary>
    /// Points on the plane through <paramref name="origin"/> with unit normal
    /// <c>normalize(tiltDegrees about X then Y from +Z)</c>. RMS of a correct fit is ~0.
    /// </summary>
    public static PlaneSample TiltedPlane(double tiltDegrees = 20.0, Vector3? origin = null, int n = 11)
    {
        var o = origin ?? new Vector3(3f, -2f, 5f);
        var rx = (float)(tiltDegrees * Math.PI / 180.0);
        var ry = (float)(tiltDegrees * 0.5 * Math.PI / 180.0);
        var rot = Matrix4x4.CreateRotationX(rx) * Matrix4x4.CreateRotationY(ry);

        var u = Vector3.TransformNormal(Vector3.UnitX, rot);
        var v = Vector3.TransformNormal(Vector3.UnitY, rot);
        var normal = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, rot));

        var pts = new List<Vector3>(n * n);
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                var s = (i - (n - 1) / 2f) * 0.5f;
                var t = (j - (n - 1) / 2f) * 0.5f;
                pts.Add(o + (u * s) + (v * t));
            }
        }

        return new PlaneSample(pts, normal, o);
    }

    /// <summary>Two hole rims in the z=0 plane, centers on the X axis a known distance apart.</summary>
    public static TwoHoleSample TwoHolePlate(float radius = 2.5f, float separation = 20f, int rimPoints = 48)
    {
        var ca = new Vector3(-separation / 2f, 0f, 0f);
        var cb = new Vector3(separation / 2f, 0f, 0f);
        return new TwoHoleSample(
            Ring(ca, radius, rimPoints),
            Ring(cb, radius, rimPoints),
            ca, cb, radius, Vector3.UnitZ);
    }

    /// <summary>A box-shaped cloud elongated along a known primary axis (X here), with its AABB.</summary>
    public static CloudSample SkewedCloud(int n = 600, int seed = 1234)
    {
        var rng = new Random(seed);
        var pts = new List<Vector3>(n);
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < n; i++)
        {
            // Elongated 6:2:1 along X:Y:Z — PCA primary axis must be X.
            var p = new Vector3(
                (float)(rng.NextDouble() - 0.5) * 12f,
                (float)(rng.NextDouble() - 0.5) * 4f,
                (float)(rng.NextDouble() - 0.5) * 2f);
            pts.Add(p);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        return new CloudSample(pts, Vector3.UnitX, new Aabb(min, max));
    }

    private static List<Vector3> Ring(Vector3 center, float radius, int count)
    {
        var pts = new List<Vector3>(count);
        for (var i = 0; i < count; i++)
        {
            var a = i / (float)count * MathF.Tau;
            pts.Add(center + new Vector3(MathF.Cos(a) * radius, MathF.Sin(a) * radius, 0f));
        }

        return pts;
    }
}
