using ScanAlign.App.Services;
using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Tests.App;

public class CentroidGroupingTests : IDisposable
{
    private readonly string _path;

    public CentroidGroupingTests()
    {
        _path = Path.Combine(Path.GetTempPath(), "sa-centroid-" + Guid.NewGuid().ToString("N") + ".obj");
        using var fs = File.Create(_path);
        new ObjWriter().Write(fs, MeshData.PointCloud(new[] { Vector3.Zero, new Vector3(30, 30, 30) }), WriteOptions.Default);
    }

    public void Dispose() => File.Delete(_path);

    private static SceneService NewService() => new(
        new MeshFormatRegistry(), new AlignmentToolRegistry(), new UnitDetector(), new BoundingBox());

    [Fact]
    public async Task Centroid_origin_averages_a_cluster_into_one_center()
    {
        var svc = NewService();
        await svc.LoadAsync(_path);
        svc.SelectTool("centroid-origin");
        Assert.True(svc.IsCentroidTool);

        // Four points around (10, 0, 0) — the average is the center.
        foreach (var p in new[] { new Vector3(12, 0, 0), new Vector3(8, 0, 0), new Vector3(10, 2, 0), new Vector3(10, -2, 0) })
        {
            svc.AddPick(new Datum(DatumKind.Point, p));
        }

        Assert.Single(svc.Picks);                       // one center, not four picks
        Assert.Equal(4, svc.CurrentClusterSize);
        Assert.True(Vector3.Distance(svc.Picks[0].Position, new Vector3(10, 0, 0)) < 1e-4f);
        Assert.True(svc.Proposal!.IsComplete);
    }

    [Fact]
    public async Task Two_centers_via_new_center_make_a_line()
    {
        var svc = NewService();
        await svc.LoadAsync(_path);
        svc.SelectTool("centroid-line");

        svc.AddPick(new Datum(DatumKind.Point, new Vector3(-1, 0, 0)));
        svc.AddPick(new Datum(DatumKind.Point, new Vector3(1, 0, 0)));   // center A ~ (0,0,0)
        Assert.False(svc.Proposal!.IsComplete);                          // still one center
        Assert.Single(svc.Picks);

        svc.StartNewCenter();
        svc.AddPick(new Datum(DatumKind.Point, new Vector3(19, 0, 0)));
        svc.AddPick(new Datum(DatumKind.Point, new Vector3(21, 0, 0)));  // center B ~ (20,0,0)

        Assert.Equal(2, svc.Picks.Count);
        Assert.True(svc.Proposal!.IsComplete);
    }
}
