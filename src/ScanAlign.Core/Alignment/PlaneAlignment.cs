using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>Shared proposal-building for the plane-based tools (3-point and best-fit).</summary>
internal static class PlaneAlignment
{
    public static AlignmentProposal Build(PlaneFitResult fit, AlignmentTarget target, string label)
    {
        var targetNormal = AlignmentMath.TargetDirection(target.Kind);
        var rotation = AlignmentMath.RotationFromTo(fit.Plane.Normal, targetNormal)
            * AlignmentMath.FlipRotation(target.FlipX, target.FlipY, target.FlipZ);
        var reference = fit.Plane.Point;

        var finalRef = AlignmentMath.MovesToOrigin(target.Origin) ? Vector3.Zero : reference;
        if (target.Placement == AxisPlacement.OnAxis)
        {
            // Coincident: drop the component along the normal so the face lies *in* the target plane.
            finalRef -= targetNormal * Vector3.Dot(finalRef, targetNormal);
        }

        var transform = AlignmentMath.ComposeOriented(rotation, reference, finalRef);
        var explanation = $"{label} → {target.Kind} (RMS {fit.Rms:0.###})";
        return new AlignmentProposal(transform, fit.Rms, explanation, IsComplete: true);
    }
}
