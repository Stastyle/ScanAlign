using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// The classic 3-2-1 (GD&amp;T datum reference frame) fixturing scheme, fully constraining all six
/// degrees of freedom from six points: a primary plane (3 points) sets two rotations and a
/// translation, a secondary line (2 points) sets the spin about the primary axis, and a tertiary
/// point anchors the last translation. Deterministic and machinist-friendly.
/// </summary>
public sealed class ThreeTwoOneTool : IAlignmentTool
{
    private readonly IPlaneFitter _planeFitter;

    public ThreeTwoOneTool()
        : this(new PlaneFitter())
    {
    }

    public ThreeTwoOneTool(IPlaneFitter planeFitter) => _planeFitter = planeFitter;

    public string Id => "three-two-one";
    public string Name => "3-2-1 fixturing";
    public int RequiredPicks => 6;
    public DatumKind ExpectedDatum => DatumKind.Point;

    public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target)
    {
        if (picks.Count < RequiredPicks)
        {
            var stage = picks.Count switch
            {
                < 3 => $"Primary plane: click 3 points on the main face ({picks.Count}/3).",
                < 5 => $"Secondary line: click 2 points along an edge ({picks.Count - 3}/2).",
                _ => "Tertiary: click 1 point to anchor the origin.",
            };
            return AlignmentProposal.Pending(stage);
        }

        // Primary: fit the plane and rotate its normal onto the primary target direction.
        var fit = _planeFitter.Fit(new[] { picks[0].Position, picks[1].Position, picks[2].Position });
        var primaryDir = AlignmentMath.TargetDirection(target.Kind);
        if (target.Flip)
        {
            primaryDir = -primaryDir;
        }

        var r1 = AlignmentMath.RotationFromTo(fit.Plane.Normal, primaryDir);

        // Secondary: spin about the primary axis so the (in-plane) edge direction hits the secondary axis.
        var secondaryDir = primaryDir == Vector3.UnitX ? Vector3.UnitY : Vector3.UnitX;
        var edge = Vector3.Normalize(Vector3.TransformNormal(picks[4].Position - picks[3].Position, r1));
        var projected = edge - (primaryDir * Vector3.Dot(edge, primaryDir));

        var r2 = Matrix4x4.Identity;
        if (projected.LengthSquared() > 1e-10f)
        {
            projected = Vector3.Normalize(projected);
            var angle = MathF.Atan2(
                Vector3.Dot(primaryDir, Vector3.Cross(projected, secondaryDir)),
                Vector3.Dot(projected, secondaryDir));
            r2 = Matrix4x4.CreateFromAxisAngle(primaryDir, angle);
        }

        var rotation = r1 * r2;

        // Tertiary point anchors the origin when an explicit-point policy is chosen; otherwise the
        // primary plane point does.
        var reference = target.Origin == OriginPolicy.PickedPoint ? picks[5].Position : picks[0].Position;
        var transform = AlignmentMath.Compose(rotation, reference, target.Origin);

        return new AlignmentProposal(transform, fit.Rms, $"3-2-1 fixturing → {target.Kind} (RMS {fit.Rms:0.###})", IsComplete: true);
    }
}
