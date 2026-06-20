using MathNet.Numerics.LinearAlgebra.Double;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>
/// Fits a circle to rim points in 3D — the basis for hole-center detection. The points are
/// projected onto their best-fit plane, an algebraic (Kåsa) least-squares circle is solved in 2D,
/// and the center is mapped back to 3D. RMS is the radial residual of the rim points.
/// </summary>
public sealed class CircleFitter : ICircleFitter
{
    public CircleFitResult Fit(IReadOnlyList<Vector3> rimPoints)
    {
        ArgumentNullException.ThrowIfNull(rimPoints);
        if (rimPoints.Count < 3)
        {
            throw new ArgumentException("Need at least three rim points to fit a circle.", nameof(rimPoints));
        }

        var frame = PrincipalComponents.Compute(rimPoints);
        var origin = frame.Centroid;
        var u = frame.Primary.Direction;    // in-plane
        var v = frame.Secondary.Direction;  // in-plane
        var normal = frame.Tertiary.Direction;

        // Kåsa fit: x^2 + y^2 + D*x + E*y + F = 0, solved via 3x3 normal equations.
        var ata = new double[3, 3];
        var atb = new double[3];
        foreach (var p in rimPoints)
        {
            var rel = p - origin;
            double x = Vector3.Dot(rel, u);
            double y = Vector3.Dot(rel, v);
            var row = new[] { x, y, 1.0 };
            var b = -(x * x + y * y);
            for (var r = 0; r < 3; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    ata[r, c] += row[r] * row[c];
                }

                atb[r] += row[r] * b;
            }
        }

        var sol = DenseMatrix.OfArray(ata).Solve(DenseVector.OfArray(atb));
        double cx = -sol[0] / 2.0, cy = -sol[1] / 2.0;
        var radius = Math.Sqrt(Math.Max(0.0, (cx * cx) + (cy * cy) - sol[2]));

        var center = origin + (u * (float)cx) + (v * (float)cy);

        double sumSq = 0;
        foreach (var p in rimPoints)
        {
            var rel = p - origin;
            double x = Vector3.Dot(rel, u) - cx;
            double y = Vector3.Dot(rel, v) - cy;
            var residual = Math.Sqrt((x * x) + (y * y)) - radius;
            sumSq += residual * residual;
        }

        var rms = Math.Sqrt(sumSq / rimPoints.Count);
        return new CircleFitResult(new Circle3(center, normal, (float)radius), rms);
    }
}
