namespace ScanAlign.Core.Solvers;

/// <summary>
/// Computes a pure rotation that maps a point set's principal axes onto world XYZ (primary→X,
/// secondary→Y, tertiary→Z) — the "PCA auto pre-align" starting pose. The result is forced to a
/// proper rotation (right-handed, determinant +1) so it never mirrors the geometry.
/// </summary>
public sealed class PcaAligner : IPcaAligner
{
    public Matrix4x4 PrincipalAxesToWorld(IReadOnlyList<Vector3> points)
    {
        var frame = PrincipalComponents.Compute(points);
        var e1 = frame.Primary.Direction;
        var e2 = frame.Secondary.Direction;

        // Re-derive the third axis to guarantee a right-handed (non-mirroring) frame.
        var e3 = Vector3.Normalize(Vector3.Cross(e1, e2));
        e2 = Vector3.Normalize(Vector3.Cross(e3, e1));

        // Row-vector convention: result component i = dot(v, column_i). Putting the principal
        // axes in the columns makes e1->X, e2->Y, e3->Z.
        return new Matrix4x4(
            e1.X, e2.X, e3.X, 0f,
            e1.Y, e2.Y, e3.Y, 0f,
            e1.Z, e2.Z, e3.Z, 0f,
            0f, 0f, 0f, 1f);
    }
}
