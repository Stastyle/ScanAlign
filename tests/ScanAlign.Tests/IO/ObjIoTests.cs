using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;

namespace ScanAlign.Tests.IO;

public class ObjIoTests
{
    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private static MeshData ReadObj(string path)
    {
        using var fs = File.OpenRead(path);
        return new ObjReader().Read(fs, ReadOptions.Default);
    }

    [Fact]
    public void Reads_cube_fixture_with_correct_counts()
    {
        var mesh = ReadObj(FixturePath("cube.obj"));

        Assert.Equal(8, mesh.Vertices.Count);
        Assert.Equal(12, mesh.TriangleCount);
        Assert.False(mesh.IsPointCloud);
        Assert.Contains(new Vector3(1, 1, 1), mesh.Vertices);
    }

    [Fact]
    public void Round_trips_positions_and_topology()
    {
        var original = ReadObj(FixturePath("cube.obj"));

        using var ms = new MemoryStream();
        new ObjWriter().Write(ms, original, WriteOptions.Default);
        ms.Position = 0;
        var reloaded = new ObjReader().Read(ms, ReadOptions.Default);

        Assert.Equal(original.Vertices, reloaded.Vertices);
        Assert.Equal(original.Indices, reloaded.Indices);
    }

    [Fact]
    public void Round_trips_per_vertex_normals()
    {
        var mesh = new MeshData(
            new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) },
            new[] { 0, 1, 2 },
            Normals: new[]
            {
                Vector3.Normalize(new Vector3(0.1f, 0.2f, 0.97f)),
                Vector3.Normalize(new Vector3(0.3f, 0.1f, 0.9f)),
                Vector3.UnitZ,
            },
            Colors: null, Unit.Millimeter, SourcePath: null);

        using var ms = new MemoryStream();
        new ObjWriter().Write(ms, mesh, WriteOptions.Default);
        ms.Position = 0;
        var reloaded = new ObjReader().Read(ms, ReadOptions.Default);

        Assert.NotNull(reloaded.Normals);
        Assert.Equal(mesh.Normals!, reloaded.Normals!);
    }

    [Fact]
    public void Triangulates_polygon_faces()
    {
        var obj = "v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf 1 2 3 4\n";
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(obj));

        var mesh = new ObjReader().Read(ms, ReadOptions.Default);

        Assert.Equal(2, mesh.TriangleCount); // a quad becomes two triangles
    }

    [Fact]
    public void Registry_resolves_obj_extension()
    {
        var registry = new MeshFormatRegistry(); // scans Core
        Assert.IsType<ObjReader>(registry.ReaderFor("part.obj"));
        Assert.IsType<ObjWriter>(registry.WriterFor(".obj"));
    }
}
