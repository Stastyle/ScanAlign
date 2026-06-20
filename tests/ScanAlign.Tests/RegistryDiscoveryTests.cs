using ScanAlign.Core.Alignment;
using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;

namespace ScanAlign.Tests;

/// <summary>
/// Proves the reflection-discovery mechanism that every fan-out track relies on: implementing a
/// contract interface is enough to be picked up — no central registration. Uses test-local dummy
/// implementations so the proof does not depend on any track being finished yet.
/// </summary>
public class RegistryDiscoveryTests
{
    private static readonly System.Reflection.Assembly ThisAssembly = typeof(RegistryDiscoveryTests).Assembly;

    [Fact]
    public void MeshFormatRegistry_discovers_reader_and_writer_by_extension()
    {
        var registry = new MeshFormatRegistry(new[] { ThisAssembly });

        Assert.Contains(".dummy", registry.ReadableExtensions);
        Assert.Contains(".dummy", registry.WritableExtensions);
        Assert.IsType<DummyReader>(registry.ReaderFor("model.DUMMY")); // case-insensitive, path ok
        Assert.IsType<DummyWriter>(registry.WriterFor(".dummy"));
        Assert.Null(registry.ReaderFor(".nope"));
    }

    [Fact]
    public void AlignmentToolRegistry_discovers_tool_by_id()
    {
        var registry = new AlignmentToolRegistry(new[] { ThisAssembly });

        Assert.Contains(registry.Tools, t => t.Id == "dummy-tool");
        Assert.IsType<DummyTool>(registry.ById("dummy-tool"));
        Assert.Null(registry.ById("missing"));
    }

    private sealed class DummyReader : IMeshReader
    {
        public IReadOnlyList<string> Extensions => new[] { ".dummy" };

        public MeshData Read(Stream stream, ReadOptions options) =>
            MeshData.PointCloud(Array.Empty<Vector3>());
    }

    private sealed class DummyWriter : IMeshWriter
    {
        public IReadOnlyList<string> Extensions => new[] { ".dummy" };

        public void Write(Stream stream, MeshData mesh, WriteOptions options)
        {
        }
    }

    private sealed class DummyTool : IAlignmentTool
    {
        public string Id => "dummy-tool";
        public string Name => "Dummy";
        public int RequiredPicks => 1;
        public DatumKind ExpectedDatum => DatumKind.Point;

        public AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target) =>
            AlignmentProposal.Pending("dummy");
    }
}
