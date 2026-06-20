using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// One-click pre-align: orient the whole mesh so its principal axes line up with world XYZ. No
/// picking — the orchestration supplies the point cloud as the single datum's
/// <see cref="Datum.SupportPoints"/>. A good "good enough" starting pose to refine from.
/// </summary>
public sealed class PcaAutoTool : IAlignmentTool
{
    private readonly IPcaAligner _aligner;

    public PcaAutoTool()
        : this(new PcaAligner())
    {
    }

    public PcaAutoTool(IPcaAligner aligner) => _aligner = aligner;

    public string Id => "pca-auto";
    public string Name => "PCA auto";
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.PlaneRegion;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        var cloud = picks.Count > 0 ? picks[0].SupportPoints : null;
        if (cloud is null || cloud.Count < 3)
        {
            return AlignmentProposal.Pending("Load a mesh to auto-align.");
        }

        var rotation = _aligner.PrincipalAxesToWorld(cloud);
        var centroid = PrincipalComponents.Centroid(cloud);
        var transform = AlignmentMath.Compose(rotation, centroid, target.Origin);

        return new AlignmentProposal(transform, 0.0, "PCA auto pre-align", IsComplete: true);
    }
}
