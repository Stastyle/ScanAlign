using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>
/// Best-fit plane. Exactly 3 points => the plane through them (RMS 0). More points => the
/// least-squares plane (the PCA axis of smallest variance is the normal), with the RMS of the
/// point-to-plane distances reported as the fit quality.
/// </summary>
public sealed class PlaneFitter : IPlaneFitter
{
    public PlaneFitResult Fit(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
        {
            throw new ArgumentException("Need at least three points to fit a plane.", nameof(points));
        }

        if (points.Count == 3)
        {
            var n = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            if (n.LengthSquared() < 1e-20f)
            {
                throw new ArgumentException("The three points are collinear; no plane is defined.", nameof(points));
            }

            return new PlaneFitResult(new Plane(points[0], Vector3.Normalize(n)), 0.0);
        }

        var frame = PrincipalComponents.Compute(points);
        var normal = frame.Tertiary.Direction; // smallest variance == plane normal
        var plane = new Plane(frame.Centroid, normal);

        double sumSq = 0;
        foreach (var p in points)
        {
            var d = Vector3.Dot(p - frame.Centroid, normal);
            sumSq += d * d;
        }

        var rms = Math.Sqrt(sumSq / points.Count);
        return new PlaneFitResult(plane, rms);
    }
}
