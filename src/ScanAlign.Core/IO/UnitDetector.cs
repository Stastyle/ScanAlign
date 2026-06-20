using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Suggests a mesh's length unit from the magnitude of its largest bounding-box dimension. For each
/// candidate unit we ask: "if the numbers were in this unit, how big would the object physically be?"
/// and pick the unit landing closest (in log scale) to a typical scanned-object size, provided it's
/// within a plausible window. Ambiguous/degenerate cases return <see cref="Unit.Unknown"/>. This only
/// suggests — the app confirms before any rescale.
/// </summary>
public sealed class UnitDetector : IUnitDetector
{
    private const double IdealMm = 150.0;   // a typical handheld-scan object (~15 cm)
    private const double MinMm = 1.0;       // smaller than this is implausible as a whole scan
    private const double MaxMm = 4000.0;    // larger than this (4 m) is implausible

    private static readonly (Unit Unit, double ToMm)[] Candidates =
    {
        (Unit.Millimeter, 1.0),
        (Unit.Centimeter, 10.0),
        (Unit.Meter, 1000.0),
        (Unit.Inch, 25.4),
    };

    public Unit Detect(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.Vertices.Count == 0)
        {
            return Unit.Unknown;
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in mesh.Vertices)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        var size = max - min;
        double maxDim = Math.Max(size.X, Math.Max(size.Y, size.Z));
        if (maxDim <= 0)
        {
            return Unit.Unknown;
        }

        var best = Unit.Unknown;
        var bestScore = double.MaxValue;
        foreach (var (unit, toMm) in Candidates)
        {
            var sizeMm = maxDim * toMm;
            if (sizeMm < MinMm || sizeMm > MaxMm)
            {
                continue;
            }

            var score = Math.Abs(Math.Log(sizeMm / IdealMm));
            if (score < bestScore)
            {
                bestScore = score;
                best = unit;
            }
        }

        return best;
    }
}
