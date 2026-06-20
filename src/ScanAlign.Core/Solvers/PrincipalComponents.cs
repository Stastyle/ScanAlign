using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ScanAlign.Core.Solvers;

/// <summary>
/// Shared PCA primitive used by the plane/line/PCA-align solvers: the centroid plus the three
/// principal axes of a point set, sorted by descending variance. Built on the covariance matrix's
/// symmetric eigen-decomposition.
/// </summary>
public static class PrincipalComponents
{
    /// <summary>One principal axis: its (unit) direction and the variance (eigenvalue) along it.</summary>
    public readonly record struct Axis(Vector3 Direction, double Variance);

    /// <summary>The PCA frame of a point set.</summary>
    public readonly record struct Frame(Vector3 Centroid, Axis Primary, Axis Secondary, Axis Tertiary)
    {
        /// <summary>Axes from largest to smallest variance.</summary>
        public IReadOnlyList<Axis> Descending => new[] { Primary, Secondary, Tertiary };
    }

    /// <summary>Compute the centroid and principal axes (descending variance) of the points.</summary>
    public static Frame Compute(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            throw new ArgumentException("Need at least two points for PCA.", nameof(points));
        }

        var centroid = Centroid(points);

        // Symmetric 3x3 covariance (population).
        double cxx = 0, cyy = 0, czz = 0, cxy = 0, cxz = 0, cyz = 0;
        foreach (var p in points)
        {
            var d = p - centroid;
            cxx += d.X * d.X;
            cyy += d.Y * d.Y;
            czz += d.Z * d.Z;
            cxy += d.X * d.Y;
            cxz += d.X * d.Z;
            cyz += d.Y * d.Z;
        }

        var n = points.Count;
        var cov = DenseMatrix.OfArray(new[,]
        {
            { cxx / n, cxy / n, cxz / n },
            { cxy / n, cyy / n, cyz / n },
            { cxz / n, cyz / n, czz / n },
        });

        var evd = cov.Evd(Symmetricity.Symmetric);
        var values = evd.EigenValues;     // ascending
        var vectors = evd.EigenVectors;   // columns align with values

        // Reorder to descending variance: columns 2, 1, 0.
        return new Frame(
            centroid,
            new Axis(Column(vectors, 2), values[2].Real),
            new Axis(Column(vectors, 1), values[1].Real),
            new Axis(Column(vectors, 0), values[0].Real));
    }

    /// <summary>Arithmetic mean of the points.</summary>
    public static Vector3 Centroid(IReadOnlyList<Vector3> points)
    {
        var sum = Vector3.Zero;
        foreach (var p in points)
        {
            sum += p;
        }

        return sum / points.Count;
    }

    private static Vector3 Column(Matrix<double> m, int col) =>
        Vector3.Normalize(new Vector3((float)m[0, col], (float)m[1, col], (float)m[2, col]));
}
