using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Centers a feature by averaging many clicked points and moving that center to the origin. Mark as
/// many points as you like around a circle rim or a square pocket — the average converges on the true
/// center as you add more. The orchestration delivers the averaged point as the datum's position.
/// </summary>
public sealed class CentroidOriginTool : IAlignmentTool
{
    public string Id => "centroid-origin";
    public string Name => "Center (avg) → origin";
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.Centroid;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < 1)
        {
            return AlignmentProposal.Pending("Click points around the feature — more is more accurate.");
        }

        var count = picks[0].SupportPoints?.Count ?? 1;
        var transform = Matrix4x4.CreateTranslation(-picks[0].Position);
        return new AlignmentProposal(transform, 0.0, $"Center of {count} points → origin", IsComplete: true);
    }
}
