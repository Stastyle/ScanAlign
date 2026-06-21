using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Aligns the line between two datums to a world axis. Works for two picked points or two hole
/// centers. Honors the target's parallel-vs-on-axis placement; spin about the axis is left free.
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

        return LineAlignment.Build(picks[0].Position, picks[1].Position, target, "2-point line");
    }
}
