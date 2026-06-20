using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Aligns a face to a world plane using many sampled points (a brushed/painted region), more robust
/// on noisy scans than three points. The region's points arrive as the single datum's
/// <see cref="Datum.SupportPoints"/>.
/// </summary>
public sealed class BestFitPlaneTool : IAlignmentTool
{
    private readonly IPlaneFitter _planeFitter;

    public BestFitPlaneTool()
        : this(new PlaneFitter())
    {
    }

    public BestFitPlaneTool(IPlaneFitter planeFitter) => _planeFitter = planeFitter;

    public string Id => "best-fit-plane";
    public string Name => "Best-fit plane";
    public int RequiredPicks => 3;
    public DatumKind ExpectedDatum => DatumKind.Point;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        // Accept either a single brushed region (SupportPoints) or many individually clicked points —
        // both feed the least-squares fit, which only improves with more samples.
        var region = picks.Count > 0 ? picks[0].SupportPoints : null;
        var points = region is { Count: >= 3 } ? region : picks.Select(p => p.Position).ToList();
        if (points.Count < 3)
        {
            return AlignmentProposal.Pending($"Click points on a flat face — more is better ({points.Count}/3+).");
        }

        var fit = _planeFitter.Fit(points);
        return PlaneAlignment.Build(fit, target, "Best-fit plane");
    }
}
