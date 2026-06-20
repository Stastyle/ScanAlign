using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Aligns the line between two datums to a world axis. Works for two picked points or two hole
/// centers (the orchestration fits each hole and passes its center as the datum position). Rotation
/// only constrains the line's direction; spin about the axis is left free.
/// </summary>
public sealed class TwoPointLineTool : IAlignmentTool
{
    public string Id => "two-point-line";
    public string Name => "2-point / 2-hole line";
    public int RequiredPicks => 2;
    public DatumKind ExpectedDatum => DatumKind.Point;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < RequiredPicks)
        {
            return AlignmentProposal.Pending($"Mark 2 points or holes ({picks.Count}/{RequiredPicks}).");
        }

        var a = picks[0].Position;
        var b = picks[1].Position;
        var dir = b - a;
        if (dir.LengthSquared() < 1e-12f)
        {
            return AlignmentProposal.Pending("The two datums coincide — pick two distinct features.");
        }

        var axis = AxisDirection(target.Kind);
        var rotation = AlignmentMath.RotationFromTo(Vector3.Normalize(dir), axis);

        // Anchor at the first datum (or its midpoint for a centered policy).
        var reference = target.Origin == OriginPolicy.Keep ? a : (a + b) * 0.5f;
        var transform = AlignmentMath.Compose(rotation, reference, target.Origin);

        return new AlignmentProposal(transform, 0.0, $"2-point line → {target.Kind}", IsComplete: true);
    }

    private static Vector3 AxisDirection(TargetKind kind) => kind switch
    {
        TargetKind.AxisX or TargetKind.PlaneYZ => Vector3.UnitX,
        TargetKind.AxisY or TargetKind.PlaneXZ => Vector3.UnitY,
        _ => Vector3.UnitZ,
    };
}
