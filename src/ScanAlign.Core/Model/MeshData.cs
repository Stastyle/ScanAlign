namespace ScanAlign.Core.Model;

/// <summary>
/// The immutable, imported geometry — the single source of truth. Alignment never mutates
/// this; it is layered as transforms in a <see cref="SceneObject"/>'s <see cref="TransformStack"/>.
/// </summary>
/// <param name="Vertices">Vertex positions in the source coordinate system.</param>
/// <param name="Indices">Flat triangle index list (length is a multiple of 3). Empty => point cloud.</param>
/// <param name="Normals">Optional per-vertex normals; null if not supplied by the source.</param>
/// <param name="Colors">Optional per-vertex RGBA colors (0..1); null if not supplied.</param>
/// <param name="Unit">Declared/detected length unit.</param>
/// <param name="SourcePath">Original file path, for provenance; null for in-memory meshes.</param>
public sealed record MeshData(
    IReadOnlyList<Vector3> Vertices,
    IReadOnlyList<int> Indices,
    IReadOnlyList<Vector3>? Normals,
    IReadOnlyList<Vector4>? Colors,
    Unit Unit,
    string? SourcePath)
{
    /// <summary>True when the mesh carries no faces (a raw scanner point cloud).</summary>
    public bool IsPointCloud => Indices.Count == 0;

    /// <summary>Number of triangles (0 for a point cloud).</summary>
    public int TriangleCount => Indices.Count / 3;

    /// <summary>Convenience factory for a point cloud (no faces).</summary>
    public static MeshData PointCloud(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<Vector4>? colors = null,
        Unit unit = Unit.Unknown,
        string? sourcePath = null)
        => new(vertices, Array.Empty<int>(), null, colors, unit, sourcePath);
}
