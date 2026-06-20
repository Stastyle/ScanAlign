using ScanAlign.Core.Model;

namespace ScanAlign.Core.Measure;

/// <summary>Read-only measurement queries surfaced as viewport overlays and status-bar readouts.</summary>
public interface IMeasurements
{
    /// <summary>Straight-line distance between two points, plus its X/Y/Z components.</summary>
    DistanceResult Distance(Vector3 a, Vector3 b);

    /// <summary>Angle between two lines/axes, in degrees (0..180).</summary>
    AngleResult Angle(Line3 a, Line3 b);

    /// <summary>Diameter of a hole/circle fitted to rim points.</summary>
    DiameterResult Diameter(IReadOnlyList<Vector3> rimPoints);

    /// <summary>Axis-aligned bounding box of a point set.</summary>
    Aabb BoundingBox(IReadOnlyList<Vector3> points);
}
