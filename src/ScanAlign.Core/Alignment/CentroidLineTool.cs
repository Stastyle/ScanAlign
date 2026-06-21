using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Aligns the line between two averaged centers to an axis. Mark a cluster of points around the first
/// feature, start a new center, then mark the second cluster — the line between the two centers is
/// snapped to the chosen axis (parallel or on-axis). Ideal for two holes/pockets whose exact centers
/// you refine by clicking many rim points.
/// </summary>
public sealed class CentroidLineTool : IAlignmentTool
{
    public string Id => "centroid-line";
    public string Name => "Center-to-center line";
    public int RequiredPicks => 2;
    public DatumKind ExpectedDatum => DatumKind.Centroid;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < RequiredPicks)
        {
            var stage = picks.Count == 0
                ? "Click points around the first feature, then 'New center'."
                : "Now click points around the second feature.";
            return AlignmentProposal.Pending(stage);
        }

        return LineAlignment.Build(picks[0].Position, picks[1].Position, target, "Center-to-center line");
    }
}
