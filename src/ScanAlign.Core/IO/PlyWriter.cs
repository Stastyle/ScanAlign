using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Writes Stanford PLY. Defaults to binary little-endian (compact, exact); ASCII when
/// <see cref="WriteOptions.Binary"/> is false. Emits per-vertex normals and uchar RGBA colors when
/// present, and a face element unless the mesh is a point cloud. Provenance goes in comment lines.
/// </summary>
public sealed class PlyWriter : IMeshWriter
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".ply" };

    public void Write(Stream stream, MeshData mesh, WriteOptions options)
    {
        var hasNormals = mesh.Normals is { Count: > 0 } && mesh.Normals.Count == mesh.Vertices.Count;
        var hasColors = mesh.Colors is { Count: > 0 } && mesh.Colors.Count == mesh.Vertices.Count;
        var hasFaces = !mesh.IsPointCloud;
        var formatName = options.Binary ? "binary_little_endian" : "ascii";

        var header = new StringBuilder();
        header.Append("ply\n");
        header.Append($"format {formatName} 1.0\n");
        header.Append("comment Written by ScanAlign\n");
        if (!string.IsNullOrWhiteSpace(options.ProvenanceHeader))
        {
            foreach (var pl in options.ProvenanceHeader.Split('\n'))
            {
                header.Append("comment ").Append(pl.TrimEnd('\r')).Append('\n');
            }
        }

        header.Append($"element vertex {mesh.Vertices.Count}\n");
        header.Append("property float x\nproperty float y\nproperty float z\n");
        if (hasNormals)
        {
            header.Append("property float nx\nproperty float ny\nproperty float nz\n");
        }

        if (hasColors)
        {
            header.Append("property uchar red\nproperty uchar green\nproperty uchar blue\nproperty uchar alpha\n");
        }

        if (hasFaces)
        {
            header.Append($"element face {mesh.TriangleCount}\n");
            header.Append("property list uchar int vertex_indices\n");
        }

        header.Append("end_header\n");
        var headerBytes = Encoding.ASCII.GetBytes(header.ToString());
        stream.Write(headerBytes, 0, headerBytes.Length);

        if (options.Binary)
        {
            WriteBinaryBody(stream, mesh, hasNormals, hasColors, hasFaces);
        }
        else
        {
            WriteAsciiBody(stream, mesh, hasNormals, hasColors, hasFaces);
        }

        options.Progress?.Report(1f);
    }

    private static void WriteBinaryBody(Stream stream, MeshData mesh, bool hasNormals, bool hasColors, bool hasFaces)
    {
        using var bw = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
            if (hasNormals)
            {
                var n = mesh.Normals![i];
                bw.Write(n.X);
                bw.Write(n.Y);
                bw.Write(n.Z);
            }

            if (hasColors)
            {
                var c = mesh.Colors![i];
                bw.Write(ToByte(c.X));
                bw.Write(ToByte(c.Y));
                bw.Write(ToByte(c.Z));
                bw.Write(ToByte(c.W));
            }
        }

        if (hasFaces)
        {
            for (var i = 0; i + 2 < mesh.Indices.Count; i += 3)
            {
                bw.Write((byte)3);
                bw.Write(mesh.Indices[i]);
                bw.Write(mesh.Indices[i + 1]);
                bw.Write(mesh.Indices[i + 2]);
            }
        }
    }

    private static void WriteAsciiBody(Stream stream, MeshData mesh, bool hasNormals, bool hasColors, bool hasFaces)
    {
        using var w = new StreamWriter(stream, new UTF8Encoding(false), 1 << 16, leaveOpen: true) { NewLine = "\n" };
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            var sb = new StringBuilder();
            sb.Append(F(v.X)).Append(' ').Append(F(v.Y)).Append(' ').Append(F(v.Z));
            if (hasNormals)
            {
                var n = mesh.Normals![i];
                sb.Append(' ').Append(F(n.X)).Append(' ').Append(F(n.Y)).Append(' ').Append(F(n.Z));
            }

            if (hasColors)
            {
                var c = mesh.Colors![i];
                sb.Append(' ').Append(ToByte(c.X)).Append(' ').Append(ToByte(c.Y)).Append(' ').Append(ToByte(c.Z)).Append(' ').Append(ToByte(c.W));
            }

            w.WriteLine(sb.ToString());
        }

        if (hasFaces)
        {
            for (var i = 0; i + 2 < mesh.Indices.Count; i += 3)
            {
                w.WriteLine($"3 {mesh.Indices[i]} {mesh.Indices[i + 1]} {mesh.Indices[i + 2]}");
            }
        }
    }

    private static byte ToByte(float c) => (byte)Math.Clamp(MathF.Round(c * 255f), 0f, 255f);

    private static string F(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
}
