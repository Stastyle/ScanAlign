using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Writes STL. Defaults to binary; ASCII when <see cref="WriteOptions.Binary"/> is false. Each
/// triangle's facet normal is computed geometrically from the winding (STL stores no per-vertex
/// data). Indexed geometry is expanded to independent facets.
/// </summary>
public sealed class StlWriter : IMeshWriter
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".stl" };

    public void Write(Stream stream, MeshData mesh, WriteOptions options)
    {
        if (mesh.IsPointCloud)
        {
            throw new InvalidOperationException("STL stores triangles; a point cloud cannot be written as STL.");
        }

        if (options.Binary)
        {
            WriteBinary(stream, mesh);
        }
        else
        {
            WriteAscii(stream, mesh);
        }

        options.Progress?.Report(1f);
    }

    private static void WriteBinary(Stream stream, MeshData mesh)
    {
        using var bw = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        var header = new byte[80];
        var tag = Encoding.ASCII.GetBytes("ScanAlign binary STL");
        Array.Copy(tag, header, Math.Min(tag.Length, header.Length));
        bw.Write(header);
        bw.Write((uint)mesh.TriangleCount);

        for (var i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            var a = mesh.Vertices[mesh.Indices[i]];
            var b = mesh.Vertices[mesh.Indices[i + 1]];
            var c = mesh.Vertices[mesh.Indices[i + 2]];
            var n = FaceNormal(a, b, c);

            Write(bw, n);
            Write(bw, a);
            Write(bw, b);
            Write(bw, c);
            bw.Write((ushort)0);
        }
    }

    private static void WriteAscii(Stream stream, MeshData mesh)
    {
        using var w = new StreamWriter(stream, new UTF8Encoding(false), 1 << 16, leaveOpen: true) { NewLine = "\n" };
        w.WriteLine("solid ScanAlign");
        for (var i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            var a = mesh.Vertices[mesh.Indices[i]];
            var b = mesh.Vertices[mesh.Indices[i + 1]];
            var c = mesh.Vertices[mesh.Indices[i + 2]];
            var n = FaceNormal(a, b, c);

            w.WriteLine($"  facet normal {F(n.X)} {F(n.Y)} {F(n.Z)}");
            w.WriteLine("    outer loop");
            w.WriteLine($"      vertex {F(a.X)} {F(a.Y)} {F(a.Z)}");
            w.WriteLine($"      vertex {F(b.X)} {F(b.Y)} {F(b.Z)}");
            w.WriteLine($"      vertex {F(c.X)} {F(c.Y)} {F(c.Z)}");
            w.WriteLine("    endloop");
            w.WriteLine("  endfacet");
        }

        w.WriteLine("endsolid ScanAlign");
    }

    private static Vector3 FaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var n = Vector3.Cross(b - a, c - a);
        return n.LengthSquared() < 1e-20f ? Vector3.Zero : Vector3.Normalize(n);
    }

    private static void Write(BinaryWriter bw, Vector3 v)
    {
        bw.Write(v.X);
        bw.Write(v.Y);
        bw.Write(v.Z);
    }

    private static string F(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
}
