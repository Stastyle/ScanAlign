using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>Shared proposal-building for the plane-based tools (3-point and best-fit).</summary>
internal static class PlaneAlignment
{
    public static AlignmentProposal Build(PlaneFitResult fit, AlignmentTarget target, string label)
    {
        var targetNormal = AlignmentMath.TargetDirection(target.Kind);
        var rotation = AlignmentMath.RotationFromTo(fit.Plane.Normal, targetNormal);
        var transform = AlignmentMath.Compose(rotation, fit.Plane.Point, target.Origin);

        var explanation = $"{label} → {target.Kind} (RMS {fit.Rms:0.###})";
        return new AlignmentProposal(transform, fit.Rms, explanation, IsComplete: true);
    }
}
