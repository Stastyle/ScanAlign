using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;

namespace ScanAlign.Tests.IO;

public class StlIoTests
{
    private static MeshData Cube()
    {
        using var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "cube.obj"));
        return new ObjReader().Read(fs, ReadOptions.Default);
    }

    private static List<string> TriangleKeys(MeshData m)
    {
        var keys = new List<string>();
        for (var i = 0; i + 2 < m.Indices.Count; i += 3)
        {
            keys.Add($"{m.Vertices[m.Indices[i]]}|{m.Vertices[m.Indices[i + 1]]}|{m.Vertices[m.Indices[i + 2]]}");
        }

        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    private static MeshData RoundTrip(MeshData mesh, bool binary)
    {
        using var ms = new MemoryStream();
        new StlWriter().Write(ms, mesh, WriteOptions.Default with { Binary = binary });
        ms.Position = 0;
        return new StlReader().Read(ms, ReadOptions.Default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Round_trips_cube_welding_to_eight_vertices(bool binary)
    {
        var original = Cube();
        var reloaded = RoundTrip(original, binary);

        Assert.Equal(8, reloaded.Vertices.Count);           // welded back to shared corners
        Assert.Equal(12, reloaded.TriangleCount);
        Assert.Equal(TriangleKeys(original), TriangleKeys(reloaded));
    }

    [Fact]
    public void Detects_binary_even_when_header_starts_with_solid()
    {
        // Binary STL whose 80-byte header begins with the word "solid".
        var mesh = Cube();
        using var ms = new MemoryStream();
        new StlWriter().Write(ms, mesh, WriteOptions.Default with { Binary = true });
        var buffer = ms.ToArray();
        Encoding.ASCII.GetBytes("solid").CopyTo(buffer, 0);

        using var ms2 = new MemoryStream(buffer);
        var reloaded = new StlReader().Read(ms2, ReadOptions.Default);

        Assert.Equal(12, reloaded.TriangleCount); // length rule wins over the misleading header
    }

    [Fact]
    public void Reads_ascii_authored_by_another_tool()
    {
        var stl = "solid t\nfacet normal 0 0 1\nouter loop\n" +
                  "vertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\n" +
                  "endloop\nendfacet\nendsolid t\n";
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(stl));

        var mesh = new StlReader().Read(ms, ReadOptions.Default);

        Assert.Equal(1, mesh.TriangleCount);
        Assert.Equal(3, mesh.Vertices.Count);
    }

    [Fact]
    public void Point_cloud_cannot_be_written_as_stl()
    {
        var cloud = MeshData.PointCloud(new[] { Vector3.Zero, Vector3.One });
        using var ms = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => new StlWriter().Write(ms, cloud, WriteOptions.Default));
    }

    [Fact]
    public void Registry_resolves_stl_extension()
    {
        var registry = new MeshFormatRegistry();
        Assert.IsType<StlReader>(registry.ReaderFor("part.stl"));
        Assert.IsType<StlWriter>(registry.WriterFor(".stl"));
    }
}
