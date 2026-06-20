using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>Translates a single picked point to the world origin (no rotation).</summary>
public sealed class PointToOriginTool : IAlignmentTool
{
    public string Id => "point-to-origin";
    public string Name => "Point → origin";
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.Point;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < RequiredPicks)
        {
            return AlignmentProposal.Pending("Click a point to move to the origin.");
        }

        var transform = Matrix4x4.CreateTranslation(-picks[0].Position);
        return new AlignmentProposal(transform, 0.0, "Point → origin", IsComplete: true);
    }
}
