using System.Windows.Media;
using System.Windows.Media.Media3D;
using ScanAlign.Core.Model;

namespace ScanAlign.App.Viewport;

/// <summary>
/// Converts <see cref="MeshData"/> into native WPF <see cref="MeshGeometry3D"/>, applying a world
/// transform on the way. Surfaces get computed per-vertex normals for shading; point clouds and
/// datum picks render as small octahedron markers (capped for display performance).
/// </summary>
internal static class Media3DBuilder
{
    private const int PointDisplayCap = 40000;

    public static MeshGeometry3D BuildSurface(MeshData mesh, Matrix4x4 world)
    {
        var positions = new Point3DCollection(mesh.Vertices.Count);
        var worldVerts = new Vector3[mesh.Vertices.Count];
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var w = Vector3.Transform(mesh.Vertices[i], world);
            worldVerts[i] = w;
            positions.Add(new Point3D(w.X, w.Y, w.Z));
        }

        var normals = ComputeNormals(worldVerts, mesh.Indices);
        return new MeshGeometry3D
        {
            Positions = positions,
            TriangleIndices = new Int32Collection(mesh.Indices),
            Normals = normals,
        };
    }

    public static MeshGeometry3D BuildPointCloud(MeshData mesh, Matrix4x4 world, float size)
    {
        var stride = Math.Max(1, mesh.Vertices.Count / PointDisplayCap);
        var geo = new MeshGeometry3D();
        for (var i = 0; i < mesh.Vertices.Count; i += stride)
        {
            AddOctahedron(geo, Vector3.Transform(mesh.Vertices[i], world), size);
        }

        return geo;
    }

    public static MeshGeometry3D BuildMarkers(IEnumerable<Vector3> worldPoints, float size)
    {
        var geo = new MeshGeometry3D();
        foreach (var p in worldPoints)
        {
            AddOctahedron(geo, p, size);
        }

        return geo;
    }

    private static Vector3DCollection ComputeNormals(Vector3[] verts, IReadOnlyList<int> indices)
    {
        var acc = new Vector3[verts.Length];
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
            var n = Vector3.Cross(verts[ib] - verts[ia], verts[ic] - verts[ia]);
            acc[ia] += n;
            acc[ib] += n;
            acc[ic] += n;
        }

        var normals = new Vector3DCollection(verts.Length);
        foreach (var n in acc)
        {
            var v = n.LengthSquared() > 1e-20f ? Vector3.Normalize(n) : Vector3.UnitZ;
            normals.Add(new Vector3D(v.X, v.Y, v.Z));
        }

        return normals;
    }

    private static void AddOctahedron(MeshGeometry3D geo, Vector3 c, float s)
    {
        var baseIndex = geo.Positions.Count;
        Span<Vector3> v =
        [
            c + new Vector3(s, 0, 0), c + new Vector3(-s, 0, 0),
            c + new Vector3(0, s, 0), c + new Vector3(0, -s, 0),
            c + new Vector3(0, 0, s), c + new Vector3(0, 0, -s),
        ];
        foreach (var p in v)
        {
            geo.Positions.Add(new Point3D(p.X, p.Y, p.Z));
        }

        ReadOnlySpan<int> tris =
        [
            4, 0, 2, 4, 2, 1, 4, 1, 3, 4, 3, 0,
            5, 2, 0, 5, 1, 2, 5, 3, 1, 5, 0, 3,
        ];
        foreach (var t in tris)
        {
            geo.TriangleIndices.Add(baseIndex + t);
        }
    }
}
