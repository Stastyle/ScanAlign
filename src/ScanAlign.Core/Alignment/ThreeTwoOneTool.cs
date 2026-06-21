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
        var primaryDir = AlignmentMath.TargetDirection(target.Kind);
        var secondaryDir = ResolveSecondary(target.Secondary, primaryDir);

        if (picks.Count < RequiredPicks)
        {
            var stage = picks.Count switch
            {
                < 3 => $"Primary face: click 3 points on the main flat face — sets {target.Kind} ({picks.Count}/3).",
                < 5 => $"Secondary edge: click 2 points along a straight edge — made parallel to the {target.Secondary} ({picks.Count - 3}/2).",
                _ => "Origin point: click 1 point — it becomes (0,0,0).",
            };
            return AlignmentProposal.Pending(stage);
        }

        // Primary: fit the plane and rotate its normal onto the primary target direction.
        var fit = _planeFitter.Fit(new[] { picks[0].Position, picks[1].Position, picks[2].Position });
        var r1 = AlignmentMath.RotationFromTo(fit.Plane.Normal, primaryDir);

        // Secondary: spin about the primary axis so the (in-plane) edge direction hits the secondary axis.
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

        var rotation = r1 * r2 * AlignmentMath.FlipRotation(target.FlipX, target.FlipY, target.FlipZ);

        // The tertiary point is the datum origin: rotate about it and place it at (0,0,0).
        var transform = AlignmentMath.ComposeOriented(rotation, picks[5].Position, Vector3.Zero);

        return new AlignmentProposal(
            transform, fit.Rms,
            $"3-2-1: face→{target.Kind}, edge→{target.Secondary}, origin set (RMS {fit.Rms:0.###})",
            IsComplete: true);
    }

    /// <summary>Pick the secondary in-plane axis; if it's parallel to the primary, fall back to a perpendicular one.</summary>
    private static Vector3 ResolveSecondary(TargetKind secondary, Vector3 primaryDir)
    {
        var dir = AlignmentMath.AxisDirection(secondary);
        if (MathF.Abs(Vector3.Dot(dir, primaryDir)) > 0.9f)
        {
            dir = MathF.Abs(primaryDir.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;
        }

        return dir;
    }
}
