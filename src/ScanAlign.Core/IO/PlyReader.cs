using System.Buffers.Binary;
using System.Globalization;
using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Reads Stanford PLY in ASCII and binary (little- and big-endian) forms: vertices, optional
/// per-vertex normals and RGBA colors, and faces (triangulated as a fan). A file with no face
/// element loads as a point cloud. The header is read byte-by-byte so binary payloads aren't
/// swallowed by a buffering text reader.
/// </summary>
public sealed class PlyReader : IMeshReader
{
    public IReadOnlyList<string> Extensions { get; } = new[] { ".ply" };

    public MeshData Read(Stream stream, ReadOptions options)
    {
        var format = PlyFormat.DataFormat.Ascii;
        var elements = new List<PlyFormat.Element>();
        PlyFormat.Element? current = null;

        string? line;
        if ((line = ReadHeaderLine(stream)) is null || line.Trim() != "ply")
        {
            throw new InvalidDataException("Not a PLY file (missing 'ply' magic).");
        }

        while ((line = ReadHeaderLine(stream)) is not null)
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("comment", StringComparison.Ordinal) || t.StartsWith("obj_info", StringComparison.Ordinal))
            {
                continue;
            }

            if (t == "end_header")
            {
                break;
            }

            var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case "format":
                    format = parts[1] switch
                    {
                        "ascii" => PlyFormat.DataFormat.Ascii,
                        "binary_little_endian" => PlyFormat.DataFormat.BinaryLittleEndian,
                        "binary_big_endian" => PlyFormat.DataFormat.BinaryBigEndian,
                        _ => throw new NotSupportedException($"Unknown PLY format '{parts[1]}'."),
                    };
                    break;

                case "element":
                    current = new PlyFormat.Element { Name = parts[1], Count = int.Parse(parts[2], CultureInfo.InvariantCulture) };
                    elements.Add(current);
                    break;

                case "property":
                    if (current is null)
                    {
                        throw new InvalidDataException("PLY property declared before any element.");
                    }

                    current.Properties.Add(parts[1] == "list"
                        ? new PlyFormat.Property { Name = parts[4], IsList = true, CountType = parts[2], ValueType = parts[3] }
                        : new PlyFormat.Property { Name = parts[2], ValueType = parts[1] });
                    break;
            }
        }

        return format == PlyFormat.DataFormat.Ascii
            ? ReadAscii(stream, elements)
            : ReadBinary(stream, elements, bigEndian: format == PlyFormat.DataFormat.BinaryBigEndian);
    }

    private static MeshData ReadAscii(Stream stream, List<PlyFormat.Element> elements)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1 << 16, leaveOpen: true);
        var tokens = reader.ReadToEnd().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var pos = 0;

        var b = new MeshBuilder();
        foreach (var element in elements)
        {
            var isVertex = element.Name == "vertex";
            var isFace = element.Name is "face" or "faces";
            for (var i = 0; i < element.Count; i++)
            {
                if (isVertex)
                {
                    var values = new double[element.Properties.Count];
                    for (var p = 0; p < element.Properties.Count; p++)
                    {
                        values[p] = double.Parse(tokens[pos++], CultureInfo.InvariantCulture);
                    }

                    b.AddVertex(element.Properties, values);
                }
                else if (isFace)
                {
                    var listProp = element.Properties.First(p => p.IsList);
                    var n = int.Parse(tokens[pos++], CultureInfo.InvariantCulture);
                    var idx = new int[n];
                    for (var k = 0; k < n; k++)
                    {
                        idx[k] = int.Parse(tokens[pos++], CultureInfo.InvariantCulture);
                    }

                    b.AddFace(idx);
                }
                else
                {
                    // Skip an unknown element's data.
                    foreach (var prop in element.Properties)
                    {
                        if (prop.IsList)
                        {
                            var n = int.Parse(tokens[pos++], CultureInfo.InvariantCulture);
                            pos += n;
                        }
                        else
                        {
                            pos++;
                        }
                    }
                }
            }
        }

        return b.Build();
    }

    private static MeshData ReadBinary(Stream stream, List<PlyFormat.Element> elements, bool bigEndian)
    {
        using var br = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        var b = new MeshBuilder();

        foreach (var element in elements)
        {
            var isVertex = element.Name == "vertex";
            var isFace = element.Name is "face" or "faces";
            for (var i = 0; i < element.Count; i++)
            {
                if (isVertex)
                {
                    var values = new double[element.Properties.Count];
                    for (var p = 0; p < element.Properties.Count; p++)
                    {
                        values[p] = ReadScalar(br, element.Properties[p].ValueType, bigEndian);
                    }

                    b.AddVertex(element.Properties, values);
                }
                else if (isFace)
                {
                    var listProp = element.Properties.First(p => p.IsList);
                    var n = (int)ReadScalar(br, listProp.CountType, bigEndian);
                    var idx = new int[n];
                    for (var k = 0; k < n; k++)
                    {
                        idx[k] = (int)ReadScalar(br, listProp.ValueType, bigEndian);
                    }

                    b.AddFace(idx);
                }
                else
                {
                    foreach (var prop in element.Properties)
                    {
                        if (prop.IsList)
                        {
                            var n = (int)ReadScalar(br, prop.CountType, bigEndian);
                            for (var k = 0; k < n; k++)
                            {
                                ReadScalar(br, prop.ValueType, bigEndian);
                            }
                        }
                        else
                        {
                            ReadScalar(br, prop.ValueType, bigEndian);
                        }
                    }
                }
            }
        }

        return b.Build();
    }

    private static double ReadScalar(BinaryReader br, string type, bool big)
    {
        var size = PlyFormat.SizeOf(type);
        Span<byte> buf = stackalloc byte[8];
        var slice = buf[..size];
        if (br.Read(slice) != size)
        {
            throw new EndOfStreamException("Unexpected end of PLY binary data.");
        }

        if (big)
        {
            slice.Reverse();
        }

        return type switch
        {
            "char" or "int8" => (sbyte)slice[0],
            "uchar" or "uint8" => slice[0],
            "short" or "int16" => BinaryPrimitives.ReadInt16LittleEndian(slice),
            "ushort" or "uint16" => BinaryPrimitives.ReadUInt16LittleEndian(slice),
            "int" or "int32" => BinaryPrimitives.ReadInt32LittleEndian(slice),
            "uint" or "uint32" => BinaryPrimitives.ReadUInt32LittleEndian(slice),
            "float" or "float32" => BitConverter.ToSingle(slice),
            "double" or "float64" => BitConverter.ToDouble(slice),
            _ => throw new NotSupportedException($"Unsupported PLY type '{type}'."),
        };
    }

    /// <summary>Reads one header line from the raw stream (ASCII), stopping after the newline.</summary>
    private static string? ReadHeaderLine(Stream stream)
    {
        var sb = new StringBuilder();
        int c;
        while ((c = stream.ReadByte()) != -1)
        {
            if (c == '\n')
            {
                break;
            }

            if (c != '\r')
            {
                sb.Append((char)c);
            }
        }

        return c == -1 && sb.Length == 0 ? null : sb.ToString();
    }

    /// <summary>Accumulates vertices/normals/colors/faces, then materializes a <see cref="MeshData"/>.</summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _positions = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector4> _colors = new();
        private readonly List<int> _indices = new();
        private bool _hasNormals;
        private bool _hasColors;

        public void AddVertex(List<PlyFormat.Property> props, double[] values)
        {
            double x = 0, y = 0, z = 0, nx = 0, ny = 0, nz = 0, r = 1, g = 1, b = 1, a = 1;
            var hasN = false;
            var hasC = false;

            for (var i = 0; i < props.Count; i++)
            {
                var name = props[i].Name;
                var v = values[i];
                var isFloat = PlyFormat.IsFloating(props[i].ValueType);
                switch (name)
                {
                    case "x": x = v; break;
                    case "y": y = v; break;
                    case "z": z = v; break;
                    case "nx": nx = v; hasN = true; break;
                    case "ny": ny = v; hasN = true; break;
                    case "nz": nz = v; hasN = true; break;
                    case "red" or "r": r = isFloat ? v : v / 255.0; hasC = true; break;
                    case "green" or "g": g = isFloat ? v : v / 255.0; hasC = true; break;
                    case "blue" or "b": b = isFloat ? v : v / 255.0; hasC = true; break;
                    case "alpha" or "a": a = isFloat ? v : v / 255.0; hasC = true; break;
                }
            }

            _positions.Add(new Vector3((float)x, (float)y, (float)z));
            _normals.Add(new Vector3((float)nx, (float)ny, (float)nz));
            _colors.Add(new Vector4((float)r, (float)g, (float)b, (float)a));
            _hasNormals |= hasN;
            _hasColors |= hasC;
        }

        public void AddFace(int[] idx)
        {
            for (var i = 1; i + 1 < idx.Length; i++)
            {
                _indices.Add(idx[0]);
                _indices.Add(idx[i]);
                _indices.Add(idx[i + 1]);
            }
        }

        public MeshData Build() => new(
            _positions,
            _indices,
            _hasNormals ? _normals : null,
            _hasColors ? _colors : null,
            Unit.Unknown,
            SourcePath: null);
    }
}
