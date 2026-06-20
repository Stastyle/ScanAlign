using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Reads Wavefront OBJ. Supports v / vn, faces of any arity (triangulated as a fan), and the
/// v, v/vt, v/vt/vn and v//vn index forms (1-based, with negative relative indices). Texture
/// coordinates and materials are ignored — only geometry matters for alignment.
/// </summary>
public sealed class ObjReader : IMeshReader
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".obj" };

    public MeshData Read(Stream stream, ReadOptions options)
    {
        var positions = new List<Vector3>();
        var normalsRaw = new List<Vector3>();
        var indices = new List<int>();
        var vertexNormal = new List<int>(); // per-position raw-normal index, -1 if none

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var span = line.AsSpan().Trim();
            if (span.IsEmpty || span[0] == '#')
            {
                continue;
            }

            if (span.StartsWith("v "))
            {
                positions.Add(ParseVector3(span[2..]));
                vertexNormal.Add(-1);
            }
            else if (span.StartsWith("vn "))
            {
                normalsRaw.Add(ParseVector3(span[3..]));
            }
            else if (span.StartsWith("f "))
            {
                ParseFace(span[2..], positions.Count, normalsRaw.Count, indices, vertexNormal);
            }
        }

        var normals = BuildNormals(positions.Count, normalsRaw, vertexNormal);
        return new MeshData(positions, indices, normals, Colors: null, Unit.Unknown, SourcePath: null);
    }

    private static void ParseFace(ReadOnlySpan<char> body, int vertexCount, int normalCount, List<int> indices, List<int> vertexNormal)
    {
        Span<int> pos = stackalloc int[64];
        Span<int> nor = stackalloc int[64];
        var count = 0;

        foreach (var rangeToken in Tokenize(body))
        {
            var token = body[rangeToken];
            var slash = token.IndexOf('/');
            ReadOnlySpan<char> posPart = slash < 0 ? token : token[..slash];
            var p = ResolveIndex(posPart, vertexCount);

            var n = -1;
            if (slash >= 0)
            {
                var rest = token[(slash + 1)..];
                var slash2 = rest.IndexOf('/');
                if (slash2 >= 0 && slash2 + 1 < rest.Length)
                {
                    n = ResolveIndex(rest[(slash2 + 1)..], normalCount);
                }
            }

            if (count < 64)
            {
                pos[count] = p;
                nor[count] = n;
                count++;
            }
        }

        // Fan triangulation; record the normal index used for each position.
        for (var i = 1; i + 1 < count; i++)
        {
            AddCorner(pos[0], nor[0], indices, vertexNormal);
            AddCorner(pos[i], nor[i], indices, vertexNormal);
            AddCorner(pos[i + 1], nor[i + 1], indices, vertexNormal);
        }
    }

    private static void AddCorner(int posIdx, int normalIdx, List<int> indices, List<int> vertexNormal)
    {
        indices.Add(posIdx);
        if (normalIdx >= 0 && posIdx < vertexNormal.Count)
        {
            vertexNormal[posIdx] = normalIdx;
        }
    }

    private static IReadOnlyList<Vector3>? BuildNormals(int vertexCount, List<Vector3> normalsRaw, List<int> vertexNormal)
    {
        if (normalsRaw.Count == 0 || !vertexNormal.Take(vertexCount).Any(n => n >= 0))
        {
            return null;
        }

        var normals = new Vector3[vertexCount];
        for (var i = 0; i < vertexCount; i++)
        {
            var ni = vertexNormal[i];
            normals[i] = ni >= 0 && ni < normalsRaw.Count ? normalsRaw[ni] : Vector3.Zero;
        }

        return normals;
    }

    private static int ResolveIndex(ReadOnlySpan<char> token, int count)
    {
        var v = int.Parse(token, CultureInfo.InvariantCulture);
        return v < 0 ? count + v : v - 1; // negative is relative to end; positive is 1-based
    }

    private static Vector3 ParseVector3(ReadOnlySpan<char> body)
    {
        var e = body.Trim();
        Span<float> vals = stackalloc float[3];
        var i = 0;
        foreach (var range in Tokenize(e))
        {
            if (i >= 3)
            {
                break;
            }

            vals[i++] = float.Parse(e[range], CultureInfo.InvariantCulture);
        }

        return new Vector3(vals[0], vals[1], vals[2]);
    }

    private static List<Range> Tokenize(ReadOnlySpan<char> span)
    {
        var ranges = new List<Range>();
        var i = 0;
        while (i < span.Length)
        {
            while (i < span.Length && char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            var start = i;
            while (i < span.Length && !char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (i > start)
            {
                ranges.Add(new Range(start, i));
            }
        }

        return ranges;
    }
}
