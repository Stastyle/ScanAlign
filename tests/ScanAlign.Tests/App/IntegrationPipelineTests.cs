using ScanAlign.App.Services;
using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Tests.App;

/// <summary>
/// Full real-file pipeline across every supported format: write an arbitrarily-posed box, load it
/// through <see cref="SceneService"/>, run a multi-step alignment (PCA auto → drop to floor), export,
/// reload, and confirm the geometry came out axis-aligned and resting on Z=0.
/// </summary>
public class IntegrationPipelineTests : IDisposable
{
    private readonly string _dir;

    public IntegrationPipelineTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "scanalign-int-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static SceneService NewService() => new(
        new MeshFormatRegistry(), new AlignmentToolRegistry(), new UnitDetector(), new BoundingBox());

    private static MeshData PosedBox()
    {
        // A 40 x 20 x 10 box (distinct extents → deterministic PCA axes).
        Vector3[] v =
        {
            new(0, 0, 0), new(40, 0, 0), new(40, 20, 0), new(0, 20, 0),
            new(0, 0, 10), new(40, 0, 10), new(40, 20, 10), new(0, 20, 10),
        };
        int[] idx =
        {
            0, 1, 2, 0, 2, 3, 4, 7, 6, 4, 6, 5,
            0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0,
        };
        var pose = Matrix4x4.CreateRotationX(0.6f) * Matrix4x4.CreateRotationZ(0.9f) *
                   Matrix4x4.CreateTranslation(75, 40, -15);
        var posed = v.Select(p => Vector3.Transform(p, pose)).ToArray();
        return new MeshData(posed, idx, null, null, Unit.Unknown, null);
    }

    [Theory]
    [InlineData(".obj")]
    [InlineData(".ply")]
    [InlineData(".stl")]
    public async Task Pca_then_drop_to_floor_round_trips_through_each_format(string ext)
    {
        var inputPath = Path.Combine(_dir, "in" + ext);
        using (var fs = File.Create(inputPath))
        {
            new MeshFormatRegistry().WriterFor(ext)!.Write(fs, PosedBox(), WriteOptions.Default);
        }

        var svc = NewService();
        await svc.LoadAsync(inputPath);

        svc.SelectTool("pca-auto");
        svc.Target = new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z);
        svc.Commit();

        svc.SelectTool("drop-to-floor");
        svc.Commit();

        Assert.Equal(2, svc.Current!.Stack.Steps.Count);

        var outPath = Path.Combine(_dir, "out" + ext);
        await svc.ExportAsync(outPath);

        var reload = NewService();
        await reload.LoadAsync(outPath);
        var verts = reload.Current!.Original.Vertices;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in verts)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        var size = max - min;
        Assert.True(MathF.Abs(min.Z) < 1e-2f, $"{ext}: should rest on Z=0, min.Z={min.Z}");
        Assert.True(size.X > size.Y && size.Y > size.Z, $"{ext}: axis-aligned by extent, got {size}");
        Assert.Equal(40f, size.X, 1); // long axis recovered along X
    }
}
