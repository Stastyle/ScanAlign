using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Centers a feature and moves that center to the origin. Click points around a circle rim or a
/// square pocket — the orchestration fits the center (a least-squares circle, robust to where you
/// click), so clustering clicks doesn't bias it and it converges on the true center as you add points.
/// </summary>
public sealed class CentroidOriginTool : IAlignmentTool
{
    public string Id => "centroid-origin";
    public string Name => "Center → origin";
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.Centroid;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < 1)
        {
            return AlignmentProposal.Pending("Click points around the feature — more around the edge is more accurate.");
        }

        var count = picks[0].SupportPoints?.Count ?? 1;
        var transform = Matrix4x4.CreateTranslation(-picks[0].Position);
        return new AlignmentProposal(transform, 0.0, $"Center of {count} points → origin", IsComplete: true);
    }
}
