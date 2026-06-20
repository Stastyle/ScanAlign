using System.Reflection;
using ScanAlign.Core.IO;

namespace ScanAlign.Core.Registry;

/// <summary>
/// Discovers <see cref="IMeshReader"/>/<see cref="IMeshWriter"/> implementations and resolves
/// the right one for a given file extension. Format tracks (OBJ/PLY/STL) add a class; nothing
/// here changes.
/// </summary>
public sealed class MeshFormatRegistry
{
    private readonly List<IMeshReader> _readers;
    private readonly List<IMeshWriter> _writers;

    /// <summary>Scan the given assemblies. Pass the assembly that hosts the format implementations.</summary>
    public MeshFormatRegistry(IEnumerable<Assembly> assemblies)
    {
        var asm = assemblies.ToArray();
        _readers = ReflectionDiscovery.Instantiate<IMeshReader>(asm).ToList();
        _writers = ReflectionDiscovery.Instantiate<IMeshWriter>(asm).ToList();
    }

    /// <summary>Scan the Core assembly (where the built-in formats live).</summary>
    public MeshFormatRegistry()
        : this(new[] { typeof(MeshFormatRegistry).Assembly })
    {
    }

    public IReadOnlyList<IMeshReader> Readers => _readers;

    public IReadOnlyList<IMeshWriter> Writers => _writers;

    /// <summary>Extensions (with dot) that can be read, e.g. for an Open dialog filter.</summary>
    public IReadOnlyList<string> ReadableExtensions =>
        _readers.SelectMany(r => r.Extensions).Select(Normalize).Distinct().ToList();

    /// <summary>Extensions (with dot) that can be written, e.g. for a Save dialog filter.</summary>
    public IReadOnlyList<string> WritableExtensions =>
        _writers.SelectMany(w => w.Extensions).Select(Normalize).Distinct().ToList();

    /// <summary>The reader for an extension or file path, or null if unsupported.</summary>
    public IMeshReader? ReaderFor(string extensionOrPath)
    {
        var ext = Normalize(extensionOrPath);
        return _readers.FirstOrDefault(r => r.Extensions.Any(e => Normalize(e) == ext));
    }

    /// <summary>The writer for an extension or file path, or null if unsupported.</summary>
    public IMeshWriter? WriterFor(string extensionOrPath)
    {
        var ext = Normalize(extensionOrPath);
        return _writers.FirstOrDefault(w => w.Extensions.Any(e => Normalize(e) == ext));
    }

    private static string Normalize(string extensionOrPath)
    {
        var ext = extensionOrPath.Contains('.') && extensionOrPath.LastIndexOf('.') > 0
            ? Path.GetExtension(extensionOrPath)
            : extensionOrPath;

        ext = ext.Trim();
        if (!ext.StartsWith('.'))
        {
            ext = "." + ext;
        }

        return ext.ToLowerInvariant();
    }
}
