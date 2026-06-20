using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Writes Wavefront OBJ. Emits vertices (and per-vertex normals when present, referenced as
/// <c>f a//a</c>), with the alignment provenance as comment lines. Floats use round-trippable
/// "G9" formatting so load→save→load is exact.
/// </summary>
public sealed class ObjWriter : IMeshWriter
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".obj" };

    public void Write(Stream stream, MeshData mesh, WriteOptions options)
    {
        using var w = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1 << 16, leaveOpen: true)
        {
            NewLine = "\n",
        };

        w.WriteLine("# Written by ScanAlign");
        if (!string.IsNullOrWhiteSpace(options.ProvenanceHeader))
        {
            foreach (var pl in options.ProvenanceHeader.Split('\n'))
            {
                w.WriteLine("# " + pl.TrimEnd('\r'));
            }
        }

        var hasNormals = mesh.Normals is { Count: > 0 } && mesh.Normals.Count == mesh.Vertices.Count;

        foreach (var v in mesh.Vertices)
        {
            w.WriteLine($"v {F(v.X)} {F(v.Y)} {F(v.Z)}");
        }

        if (hasNormals)
        {
            foreach (var n in mesh.Normals!)
            {
                w.WriteLine($"vn {F(n.X)} {F(n.Y)} {F(n.Z)}");
            }
        }

        for (var i = 0; i + 2 < mesh.Indices.Count; i += 3)
        {
            int a = mesh.Indices[i] + 1, b = mesh.Indices[i + 1] + 1, c = mesh.Indices[i + 2] + 1;
            w.WriteLine(hasNormals
                ? $"f {a}//{a} {b}//{b} {c}//{c}"
                : $"f {a} {b} {c}");
        }

        options.Progress?.Report(1f);
    }

    private static string F(float value) => value.ToString("G9", CultureInfo.InvariantCulture);
}
