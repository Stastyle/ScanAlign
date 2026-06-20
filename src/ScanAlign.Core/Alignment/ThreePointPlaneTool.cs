using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// Aligns a flat face to a world plane from three picked points: fit the plane through them, rotate
/// its normal onto the target plane's normal, and position per the origin policy.
/// </summary>
public sealed class ThreePointPlaneTool : IAlignmentTool
{
    private readonly IPlaneFitter _planeFitter;

    public ThreePointPlaneTool()
        : this(new PlaneFitter())
    {
    }

    public ThreePointPlaneTool(IPlaneFitter planeFitter) => _planeFitter = planeFitter;

    public string Id => "three-point-plane";
    public string Name => "3-point plane";
    public int RequiredPicks => 3;
    public DatumKind ExpectedDatum => DatumKind.Point;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < RequiredPicks)
        {
            return AlignmentProposal.Pending($"Click {RequiredPicks} points on a flat face ({picks.Count}/{RequiredPicks}).");
        }

        var pts = picks.Take(RequiredPicks).Select(p => p.Position).ToArray();
        var fit = _planeFitter.Fit(pts);
        return PlaneAlignment.Build(fit, target, "3-point plane");
    }
}
