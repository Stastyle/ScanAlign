using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;

namespace ScanAlign.Tests.IO;

public class PlyIoTests
{
    private static MeshData Triangle(bool normals = false, bool colors = false)
    {
        return new MeshData(
            new[] { new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(0, 3, 0) },
            new[] { 0, 1, 2 },
            normals ? new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ } : null,
            colors ? new[] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1), new Vector4(0, 0, 1, 1) } : null,
            Unit.Millimeter, SourcePath: null);
    }

    private static MeshData RoundTrip(MeshData mesh, bool binary)
    {
        using var ms = new MemoryStream();
        new PlyWriter().Write(ms, mesh, WriteOptions.Default with { Binary = binary });
        ms.Position = 0;
        return new PlyReader().Read(ms, ReadOptions.Default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Round_trips_positions_and_topology(bool binary)
    {
        var original = Triangle();
        var reloaded = RoundTrip(original, binary);

        Assert.Equal(original.Vertices, reloaded.Vertices);
        Assert.Equal(original.Indices, reloaded.Indices);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Round_trips_point_cloud_with_no_faces(bool binary)
    {
        var cloud = MeshData.PointCloud(new[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6) });
        var reloaded = RoundTrip(cloud, binary);

        Assert.True(reloaded.IsPointCloud);
        Assert.Equal(cloud.Vertices, reloaded.Vertices);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Round_trips_vertex_colors_within_quantization(bool binary)
    {
        var reloaded = RoundTrip(Triangle(colors: true), binary);

        Assert.NotNull(reloaded.Colors);
        Assert.Equal(new Vector4(1, 0, 0, 1), reloaded.Colors![0]);
        Assert.Equal(new Vector4(0, 1, 0, 1), reloaded.Colors[1]);
    }

    [Fact]
    public void Round_trips_normals()
    {
        var reloaded = RoundTrip(Triangle(normals: true), binary: true);
        Assert.NotNull(reloaded.Normals);
        Assert.Equal(Vector3.UnitZ, reloaded.Normals![0]);
    }

    [Fact]
    public void Reads_ascii_authored_by_another_tool()
    {
        var ply = "ply\nformat ascii 1.0\nelement vertex 3\nproperty float x\nproperty float y\n" +
                  "property float z\nelement face 1\nproperty list uchar int vertex_indices\nend_header\n" +
                  "0 0 0\n1 0 0\n0 1 0\n3 0 1 2\n";
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes(ply));

        var mesh = new PlyReader().Read(ms, ReadOptions.Default);

        Assert.Equal(3, mesh.Vertices.Count);
        Assert.Equal(1, mesh.TriangleCount);
    }

    [Fact]
    public void Reads_binary_big_endian()
    {
        var header = "ply\nformat binary_big_endian 1.0\nelement vertex 1\n" +
                     "property float x\nproperty float y\nproperty float z\nend_header\n";
        using var ms = new MemoryStream();
        var hb = Encoding.ASCII.GetBytes(header);
        ms.Write(hb, 0, hb.Length);
        foreach (var f in new[] { 1.5f, -2.25f, 3.75f })
        {
            var bytes = BitConverter.GetBytes(f);
            Array.Reverse(bytes); // emit big-endian
            ms.Write(bytes, 0, bytes.Length);
        }

        ms.Position = 0;
        var mesh = new PlyReader().Read(ms, ReadOptions.Default);

        Assert.Single(mesh.Vertices);
        Assert.Equal(new Vector3(1.5f, -2.25f, 3.75f), mesh.Vertices[0]);
    }

    [Fact]
    public void Rejects_non_ply()
    {
        using var ms = new MemoryStream(Encoding.ASCII.GetBytes("not a ply file\n"));
        Assert.Throws<InvalidDataException>(() => new PlyReader().Read(ms, ReadOptions.Default));
    }

    [Fact]
    public void Registry_resolves_ply_extension()
    {
        var registry = new MeshFormatRegistry();
        Assert.IsType<PlyReader>(registry.ReaderFor("scan.ply"));
        Assert.IsType<PlyWriter>(registry.WriterFor(".ply"));
    }
}
