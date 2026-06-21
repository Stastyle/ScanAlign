namespace ScanAlign.Core.Model;

/// <summary>What kind of geometric reference a pick represents.</summary>
public enum DatumKind
{
    /// <summary>A single picked point on the surface.</summary>
    Point,

    /// <summary>A region of points sampled on a (nominally flat) face, for plane fitting.</summary>
    PlaneRegion,

    /// <summary>An endpoint of a line/axis the user is defining.</summary>
    LineEndpoint,

    /// <summary>The center of a hole/circle (typically derived by fitting its rim).</summary>
    HoleCenter,

    /// <summary>
    /// A center derived by averaging a cluster of clicked points — more points converge on the true
    /// center of a circle or pocket. Tools expecting this kind collect points into groups.
    /// </summary>
    Centroid,
}

/// <summary>
/// A user-supplied geometric reference fed to an <c>IAlignmentTool</c>. The optional
/// <paramref name="SupportPoints"/> carry the raw samples behind a fit (e.g. rim points
/// for a hole, or face samples for a best-fit plane).
/// </summary>
public sealed record Datum(
    DatumKind Kind,
    Vector3 Position,
    Vector3? Normal = null,
    IReadOnlyList<Vector3>? SupportPoints = null);
