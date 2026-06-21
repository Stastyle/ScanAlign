using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Shared rigid-transform construction for the alignment tools: which world direction a target
/// resolves to, the minimal rotation between two directions, and how an origin policy positions the
/// reoriented geometry. All matrices follow the row-vector convention (<c>Vector3.Transform(p, M)</c>).
/// </summary>
public static class AlignmentMath
{
    /// <summary>
    /// The world direction a target maps onto: for a plane target it is the plane's normal
    /// (XY→Z, XZ→Y, YZ→X); for an axis target it is that axis.
    /// </summary>
    public static Vector3 TargetDirection(TargetKind kind) => kind switch
    {
        TargetKind.AxisX or TargetKind.PlaneYZ => Vector3.UnitX,
        TargetKind.AxisY or TargetKind.PlaneXZ => Vector3.UnitY,
        TargetKind.AxisZ or TargetKind.PlaneXY => Vector3.UnitZ,
        _ => Vector3.UnitZ,
    };

    /// <summary>Minimal rotation taking unit vector <paramref name="from"/> onto <paramref name="to"/>.</summary>
    public static Matrix4x4 RotationFromTo(Vector3 from, Vector3 to)
    {
        from = Vector3.Normalize(from);
        to = Vector3.Normalize(to);

        var cos = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);
        if (cos > 0.9999999f)
        {
            return Matrix4x4.Identity;
        }

        if (cos < -0.9999999f)
        {
            // Antiparallel: rotate 180° about any axis perpendicular to 'from'.
            var perp = Vector3.Cross(from, Vector3.UnitX);
            if (perp.LengthSquared() < 1e-12f)
            {
                perp = Vector3.Cross(from, Vector3.UnitY);
            }

            return Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(perp), MathF.PI);
        }

        var axis = Vector3.Normalize(Vector3.Cross(from, to));
        var angle = MathF.Acos(cos);
        return Matrix4x4.CreateFromAxisAngle(axis, angle);
    }

    /// <summary>
    /// Compose the full transform: reorient by <paramref name="rotation"/> and place the geometry per
    /// <paramref name="origin"/>. <paramref name="reference"/> is the datum's anchor point (plane point,
    /// line point, or picked point).
    /// </summary>
    public static Matrix4x4 Compose(Matrix4x4 rotation, Vector3 reference, OriginPolicy origin)
    {
        return origin switch
        {
            // Anchor the reference point at the world origin.
            OriginPolicy.PlaneOrigin or OriginPolicy.PickedPoint or OriginPolicy.BBoxCenter =>
                Matrix4x4.CreateTranslation(-reference) * rotation,

            // Keep the object roughly in place: rotate about the reference point.
            _ => Matrix4x4.CreateTranslation(-reference) * rotation * Matrix4x4.CreateTranslation(reference),
        };
    }

    /// <summary>
    /// Compose so that, after rotation, the anchor <paramref name="reference"/> lands exactly at
    /// <paramref name="finalReference"/>. This drives the parallel-vs-on-axis placement: callers
    /// choose the final anchor (its current spot, the origin, or its projection onto the target).
    /// </summary>
    public static Matrix4x4 ComposeOriented(Matrix4x4 rotation, Vector3 reference, Vector3 finalReference) =>
        Matrix4x4.CreateTranslation(-reference) * rotation * Matrix4x4.CreateTranslation(finalReference);

    /// <summary>True when the origin policy means "move the anchor to the world origin".</summary>
    public static bool MovesToOrigin(OriginPolicy origin) =>
        origin is OriginPolicy.PlaneOrigin or OriginPolicy.PickedPoint or OriginPolicy.BBoxCenter;

    /// <summary>
    /// A composite 180° flip about any combination of world axes — applied after alignment to flip the
    /// part's facing. (Note two flips equal the third, so this spans the four valid orientations.)
    /// </summary>
    public static Matrix4x4 FlipRotation(bool flipX, bool flipY, bool flipZ)
    {
        var m = Matrix4x4.Identity;
        if (flipX)
        {
            m *= Matrix4x4.CreateRotationX(MathF.PI);
        }

        if (flipY)
        {
            m *= Matrix4x4.CreateRotationY(MathF.PI);
        }

        if (flipZ)
        {
            m *= Matrix4x4.CreateRotationZ(MathF.PI);
        }

        return m;
    }

    /// <summary>The world axis a target resolves to for line alignment (X/Y/Z).</summary>
    public static Vector3 AxisDirection(TargetKind kind) => kind switch
    {
        TargetKind.AxisX or TargetKind.PlaneYZ => Vector3.UnitX,
        TargetKind.AxisY or TargetKind.PlaneXZ => Vector3.UnitY,
        _ => Vector3.UnitZ,
    };
}
