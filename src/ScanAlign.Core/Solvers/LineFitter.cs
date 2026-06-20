using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>
/// Best-fit line (axis) through points: the PCA axis of largest variance passing through the
/// centroid. RMS is the perpendicular point-to-line distance.
/// </summary>
public sealed class LineFitter : ILineFitter
{
    public LineFitResult Fit(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 2)
        {
            throw new ArgumentException("Need at least two points to fit a line.", nameof(points));
        }

        if (points.Count == 2)
        {
            var dir2 = points[1] - points[0];
            if (dir2.LengthSquared() < 1e-20f)
            {
                throw new ArgumentException("The two points are coincident; no line is defined.", nameof(points));
            }

            return new LineFitResult(new Line3(points[0], Vector3.Normalize(dir2)), 0.0);
        }

        var frame = PrincipalComponents.Compute(points);
        var dir = frame.Primary.Direction; // largest variance == line direction
        var line = new Line3(frame.Centroid, dir);

        double sumSq = 0;
        foreach (var p in points)
        {
            var rel = p - frame.Centroid;
            var along = Vector3.Dot(rel, dir);
            var perp = rel - (dir * along);
            sumSq += perp.LengthSquared();
        }

        var rms = Math.Sqrt(sumSq / points.Count);
        return new LineFitResult(line, rms);
    }
}
