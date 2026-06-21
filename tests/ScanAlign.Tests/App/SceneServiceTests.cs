using ScanAlign.App.Services;
using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;
using ScanAlign.Core.Solvers;
using ScanAlign.Tests.Support;

namespace ScanAlign.Tests.App;

public class SceneServiceTests : IDisposable
{
    private readonly string _dir;

    public SceneServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "scanalign-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static SceneService NewService() => new(
        new MeshFormatRegistry(),
        new AlignmentToolRegistry(),
        new UnitDetector(),
        new BoundingBox(),
        new CircleFitter());

    private string WritePointCloudObj(IEnumerable<Vector3> points, string name)
    {
        var path = Path.Combine(_dir, name);
        using var fs = File.Create(path);
        new ObjWriter().Write(fs, MeshData.PointCloud(points.ToList()), WriteOptions.Default);
        return path;
    }

    [Fact]
    public async Task Full_loop_aligns_a_tilted_face_and_round_trips_through_export()
    {
        var sample = SyntheticGeometry.TiltedPlane(tiltDegrees: 18, n: 11);
        var path = WritePointCloudObj(sample.Points, "tilted.obj");

        var svc = NewService();
        var changes = 0;
        svc.Changed += (_, _) => changes++;

        await svc.LoadAsync(path);
        Assert.NotNull(svc.Current);
        Assert.False(svc.CanUndo);

        svc.SelectTool("three-point-plane");
        svc.Target = new AlignmentTarget(TargetKind.PlaneXY, OriginPolicy.PlaneOrigin, UpAxis.Z);

        var verts = svc.Current!.Original.Vertices;
        svc.AddPick(new Datum(DatumKind.Point, verts[0]));
        svc.AddPick(new Datum(DatumKind.Point, verts[10]));
        Assert.False(svc.Proposal!.IsComplete);          // 2/3 picks

        svc.AddPick(new Datum(DatumKind.Point, verts[^1]));
        Assert.True(svc.Proposal!.IsComplete);           // 3/3 picks

        svc.Commit();
        Assert.True(svc.CanUndo);
        Assert.Single(svc.Current!.Stack.Steps);
        Assert.NotEqual(Matrix4x4.Identity, svc.Current.World);
        Assert.Empty(svc.Picks);                         // picks cleared on commit
        Assert.True(changes > 0);

        // The aligned face is now parallel to XY (near-constant Z).
        var zs = svc.Current.ToWorld().Vertices.Select(v => v.Z).ToArray();
        Assert.True(zs.Max() - zs.Min() < 1e-3f);

        // Export → reload: alignment is baked into the file.
        var outPath = Path.Combine(_dir, "aligned.ply");
        await svc.ExportAsync(outPath);

        var reload = NewService();
        await reload.LoadAsync(outPath);
        var rz = reload.Current!.Original.Vertices.Select(v => v.Z).ToArray();
        Assert.True(rz.Max() - rz.Min() < 1e-3f);
    }

    [Fact]
    public async Task Undo_and_redo_walk_the_alignment_stack()
    {
        var path = WritePointCloudObj(SyntheticGeometry.TiltedPlane().Points, "p.obj");
        var svc = NewService();
        await svc.LoadAsync(path);

        svc.SelectTool("point-to-origin");
        svc.AddPick(new Datum(DatumKind.Point, new Vector3(5, 5, 5)));
        svc.Commit();
        Assert.NotEqual(Matrix4x4.Identity, svc.Current!.World);

        svc.Undo();
        Assert.Equal(Matrix4x4.Identity, svc.Current.World);
        Assert.True(svc.CanRedo);

        svc.Redo();
        Assert.NotEqual(Matrix4x4.Identity, svc.Current.World);

        svc.ResetAlignment();
        Assert.True(svc.Current.Stack.IsEmpty);
        Assert.False(svc.CanRedo);
    }

    [Fact]
    public async Task Pca_auto_proposes_without_picks()
    {
        var cloud = SyntheticGeometry.SkewedCloud(n: 2000, seed: 5);
        var path = WritePointCloudObj(cloud.Points, "cloud.obj");
        var svc = NewService();
        await svc.LoadAsync(path);

        svc.SelectTool("pca-auto");
        svc.Target = new AlignmentTarget(TargetKind.AxisX, OriginPolicy.Keep, UpAxis.Z);

        Assert.True(svc.Proposal!.IsComplete); // no picks needed
        svc.Commit();

        var mapped = Vector3.Normalize(Vector3.TransformNormal(cloud.PrimaryAxis, svc.Current!.World));
        Assert.True(MathF.Abs(mapped.X) > 0.99f);
    }

    [Fact]
    public async Task Detects_unit_on_load()
    {
        // ~100 mm object => Millimeter.
        var pts = new[] { Vector3.Zero, new Vector3(100, 80, 40) };
        var path = WritePointCloudObj(pts, "u.obj");
        var svc = NewService();
        await svc.LoadAsync(path);

        Assert.Equal(Unit.Millimeter, svc.Current!.Original.Unit);
    }
}
