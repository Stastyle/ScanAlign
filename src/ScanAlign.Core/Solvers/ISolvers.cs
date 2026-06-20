using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>Best-fit plane through points. 3 points => exact; N points => SVD/PCA least-squares.</summary>
public interface IPlaneFitter
{
    PlaneFitResult Fit(IReadOnlyList<Vector3> points);
}

/// <summary>Best-fit line (axis) through points via PCA.</summary>
public interface ILineFitter
{
    LineFitResult Fit(IReadOnlyList<Vector3> points);
}

/// <summary>Best-fit circle through rim points (yields hole center, normal, diameter).</summary>
public interface ICircleFitter
{
    CircleFitResult Fit(IReadOnlyList<Vector3> rimPoints);
}

/// <summary>Computes a rotation mapping a point set's principal axes onto world XYZ (PCA pre-align).</summary>
public interface IPcaAligner
{
    Matrix4x4 PrincipalAxesToWorld(IReadOnlyList<Vector3> points);
}

/// <summary>Computes the axis-aligned bounding box of a point set.</summary>
public interface IBoundingBox
{
    Aabb Compute(IReadOnlyList<Vector3> points);
}
