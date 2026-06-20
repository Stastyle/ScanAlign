using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// One-click "rest on the floor": translate the mesh straight down so its lowest point sits on the
/// Z=0 plane. No rotation — useful for 3D-print bed placement after the part is already squared.
/// The orchestration supplies the point cloud as the single datum's <see cref="Datum.SupportPoints"/>.
/// </summary>
public sealed class DropToFloorTool : IAlignmentTool
{
    public string Id => "drop-to-floor";
    public string Name => "Drop to floor";
    public int RequiredPicks => 1;
    public DatumKind ExpectedDatum => DatumKind.PlaneRegion;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        var cloud = picks.Count > 0 ? picks[0].SupportPoints : null;
        if (cloud is null || cloud.Count == 0)
        {
            return AlignmentProposal.Pending("Load a mesh to drop to the floor.");
        }

        var minZ = float.MaxValue;
        foreach (var p in cloud)
        {
            if (p.Z < minZ)
            {
                minZ = p.Z;
            }
        }

        var transform = Matrix4x4.CreateTranslation(0, 0, -minZ);
        return new AlignmentProposal(transform, 0.0, "Drop to floor (Z=0)", IsComplete: true);
    }
}
