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
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.PlaneRegion;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        var region = picks.Count > 0 ? picks[0].SupportPoints : null;
        if (region is null || region.Count < 3)
        {
            return AlignmentProposal.Pending("Brush a flat region (at least 3 points) on the face.");
        }

        var fit = _planeFitter.Fit(region);
        return PlaneAlignment.Build(fit, target, "Best-fit plane");
    }
}
