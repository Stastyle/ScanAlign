using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>Shared proposal-building for line-to-axis tools (2-point line and center-to-center line).</summary>
internal static class LineAlignment
{
    public static AlignmentProposal Build(Vector3 a, Vector3 b, AlignmentTarget target, string label)
    {
        var dir = b - a;
        if (dir.LengthSquared() < 1e-12f)
        {
            return AlignmentProposal.Pending("The two points coincide — pick two distinct features.");
        }

        var axis = AlignmentMath.AxisDirection(target.Kind);
        var rotation = AlignmentMath.RotationFromTo(Vector3.Normalize(dir), axis);

        // Anchor: the first point, or the midpoint when moving to the origin.
        var reference = target.Origin == OriginPolicy.Keep ? a : (a + b) * 0.5f;

        var finalRef = AlignmentMath.MovesToOrigin(target.Origin) ? Vector3.Zero : reference;
        if (target.Placement == AxisPlacement.OnAxis)
        {
            // Collinear with the axis: keep only the component along it (zero the perpendicular offset).
            finalRef = axis * Vector3.Dot(finalRef, axis);
        }

        var transform = AlignmentMath.ComposeOriented(rotation, reference, finalRef);
        var placement = target.Placement == AxisPlacement.OnAxis ? "on" : "parallel to";
        return new AlignmentProposal(transform, 0.0, $"{label} {placement} {target.Kind}", IsComplete: true);
    }
}
