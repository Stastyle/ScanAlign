using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>Axis-aligned bounding box of a point set.</summary>
public sealed class BoundingBox : IBoundingBox
{
    public Aabb Compute(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("Cannot compute a bounding box of an empty point set.", nameof(points));
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in points)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        return new Aabb(min, max);
    }
}
