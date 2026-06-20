using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Reads STL (binary and ASCII). STL is a soup of independent triangles, so the reader welds
/// identical vertex positions into shared, indexed vertices. Binary vs ASCII is detected by the
/// exact-length rule (84 + 50·triangles), which disambiguates even files whose header starts with
/// "solid". Per-face normals are not carried into the per-vertex model.
/// </summary>
public sealed class StlReader : IMeshReader
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".stl" };

    public MeshData Read(Stream stream, ReadOptions options)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var buffer = ms.GetBuffer();
        var length = (int)ms.Length;

        var triangles = IsBinary(buffer, length)
            ? ReadBinary(buffer, length)
            : ReadAscii(buffer, length);

        return Weld(triangles);
    }

    private static bool IsBinary(byte[] buffer, int length)
    {
        if (length < 84)
        {
            return false;
        }

        uint count = BitConverter.ToUInt32(buffer, 80);
        return 84L + (count * 50L) == length;
    }

    private static List<Vector3> ReadBinary(byte[] buffer, int length)
    {
        uint count = BitConverter.ToUInt32(buffer, 80);
        var verts = new List<Vector3>((int)count * 3);
        var offset = 84;
        for (uint i = 0; i < count && offset + 50 <= length; i++)
        {
            offset += 12; // skip the per-face normal
            for (var v = 0; v < 3; v++)
            {
                var x = BitConverter.ToSingle(buffer, offset);
                var y = BitConverter.ToSingle(buffer, offset + 4);
                var z = BitConverter.ToSingle(buffer, offset + 8);
                verts.Add(new Vector3(x, y, z));
                offset += 12;
            }

            offset += 2; // attribute byte count
        }

        return verts;
    }

    private static List<Vector3> ReadAscii(byte[] buffer, int length)
    {
        var text = Encoding.ASCII.GetString(buffer, 0, length);
        var tokens = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var verts = new List<Vector3>();
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!tokens[i].Equals("vertex", StringComparison.OrdinalIgnoreCase) || i + 3 >= tokens.Length)
            {
                continue;
            }

            verts.Add(new Vector3(
                float.Parse(tokens[i + 1], CultureInfo.InvariantCulture),
                float.Parse(tokens[i + 2], CultureInfo.InvariantCulture),
                float.Parse(tokens[i + 3], CultureInfo.InvariantCulture)));
            i += 3;
        }

        return verts;
    }

    private static MeshData Weld(List<Vector3> soup)
    {
        var map = new Dictionary<Vector3, int>();
        var vertices = new List<Vector3>();
        var indices = new List<int>(soup.Count);

        foreach (var p in soup)
        {
            if (!map.TryGetValue(p, out var idx))
            {
                idx = vertices.Count;
                map[p] = idx;
                vertices.Add(p);
            }

            indices.Add(idx);
        }

        return new MeshData(vertices, indices, Normals: null, Colors: null, Unit.Unknown, SourcePath: null);
    }
}
