using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Reads a mesh from a stream. Implementations are discovered by reflection via
/// <c>MeshFormatRegistry</c> and matched to files by <see cref="Extensions"/> — adding a new
/// format is just adding a class, no central registration.
/// </summary>
public interface IMeshReader
{
    /// <summary>Lowercase extensions this reader handles, including the dot (e.g. ".obj").</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>Parse a mesh from <paramref name="stream"/>. Must not dispose the stream.</summary>
    MeshData Read(Stream stream, ReadOptions options);
}
