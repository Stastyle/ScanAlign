using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Writes a mesh to a stream. Discovered by reflection via <c>MeshFormatRegistry</c> and matched
/// to a target file by <see cref="Extensions"/>.
/// </summary>
public interface IMeshWriter
{
    /// <summary>Lowercase extensions this writer handles, including the dot (e.g. ".ply").</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>Serialize <paramref name="mesh"/> to <paramref name="stream"/>. Must not dispose it.</summary>
    void Write(Stream stream, MeshData mesh, WriteOptions options);
}
