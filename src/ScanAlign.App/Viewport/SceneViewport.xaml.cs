using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ScanAlign.App.Services;
using ScanAlign.Core.Model;

namespace ScanAlign.App.Viewport;

/// <summary>
/// The 3D viewport: renders the current object (and a translucent preview of a complete proposal),
/// the ground grid + axis triad, and datum-pick markers. Left-drag orbits, right/middle-drag pans,
/// the wheel zooms, and a left click (without drag) adds a datum at the surface hit point.
/// </summary>
public partial class SceneViewport : UserControl
{
    private static readonly Color Teal = Color.FromRgb(0x2D, 0xD4, 0xBF);
    private static readonly Color Steel = Color.FromRgb(0x9A, 0xA0, 0xA8);

    private readonly ISceneService _scene;
    private readonly OrbitCamera _orbit = new();

    private Point _downPos;
    private Point _lastMouse;
    private bool _dragging;
    private MouseButton _dragButton;
    private SceneObject? _framed;

    public SceneViewport(ISceneService scene)
    {
        _scene = scene;
        InitializeComponent();
        BuildLights();

        _scene.Changed += (_, _) => Dispatcher.Invoke(Rebuild);
        Loaded += (_, _) =>
        {
            Rebuild();
            ApplyCamera();
        };

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
    }

    private void BuildLights()
    {
        var group = new Model3DGroup();
        group.Children.Add(new AmbientLight(Color.FromRgb(0x55, 0x57, 0x5B)));
        group.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-0.5, -0.7, -1.0)));
        group.Children.Add(new DirectionalLight(Color.FromRgb(0x60, 0x60, 0x60), new Vector3D(0.8, 0.4, 0.6)));
        LightsRoot.Content = group;
    }

    private void Rebuild()
    {
        StaticRoot.Children.Clear();
        SceneRoot.Children.Clear();

        var extent = ExtentHint();
        AddModel(StaticRoot, GridBuilder.BuildGrid(extent * 0.75f, 10), Emissive(Color.FromRgb(0x2A, 0x2E, 0x33)));
        AddModel(StaticRoot, GridBuilder.BuildAxis(Vector3.UnitX, extent * 0.6f), Emissive(Color.FromRgb(0xE2, 0x4B, 0x4A)));
        AddModel(StaticRoot, GridBuilder.BuildAxis(Vector3.UnitY, extent * 0.6f), Emissive(Color.FromRgb(0x37, 0x8A, 0xDD)));
        AddModel(StaticRoot, GridBuilder.BuildAxis(Vector3.UnitZ, extent * 0.6f), Emissive(Color.FromRgb(0x97, 0xC4, 0x59)));

        if (_scene.Current is { } scene)
        {
            var world = scene.World;
            if (scene.Original.IsPointCloud)
            {
                AddModel(SceneRoot, Media3DBuilder.BuildPointCloud(scene.Original, world, extent * 0.004f), Emissive(Steel));
            }
            else
            {
                var surface = Diffuse(Steel);
                AddModel(SceneRoot, Media3DBuilder.BuildSurface(scene.Original, world), surface, surface);
            }

            if (_scene.Proposal is { IsComplete: true } proposal && !scene.Original.IsPointCloud)
            {
                var preview = Diffuse(Color.FromArgb(0x66, Teal.R, Teal.G, Teal.B));
                AddModel(SceneRoot, Media3DBuilder.BuildSurface(scene.Original, world * proposal.Transform), preview, preview);
            }

            if (_scene.Picks.Count > 0)
            {
                AddModel(SceneRoot, Media3DBuilder.BuildMarkers(_scene.Picks.Select(p => p.Position), extent * 0.02f), Emissive(Teal));
            }

            FrameIfNew(scene);
        }

        HintText.Text = _scene.Current is null
            ? "Open a scan (OBJ · PLY · STL) to begin."
            : _scene.ActiveTool is null
                ? "Pick an alignment tool on the left."
                : _scene.Proposal?.Explanation ?? "Click on the surface to add datums.";
    }

    private float ExtentHint()
    {
        if (_scene.WorldBounds is { } b)
        {
            var s = b.Size;
            var m = MathF.Max(s.X, MathF.Max(s.Y, s.Z));
            if (m > 1e-4f)
            {
                return m;
            }
        }

        return 1f;
    }

    private void FrameIfNew(SceneObject scene)
    {
        if (ReferenceEquals(_framed, scene) || _scene.WorldBounds is not { } b)
        {
            return;
        }

        _framed = scene;
        var s = b.Size;
        var radius = 0.5f * MathF.Sqrt((s.X * s.X) + (s.Y * s.Y) + (s.Z * s.Z));
        _orbit.FrameExtents(b.Center, radius);
        ApplyCamera();
    }

    private void ApplyCamera()
    {
        var p = _orbit.Position;
        var l = _orbit.LookDirection;
        var u = _orbit.Up;
        Cam.Position = new Point3D(p.X, p.Y, p.Z);
        Cam.LookDirection = new Vector3D(l.X, l.Y, l.Z);
        Cam.UpDirection = new Vector3D(u.X, u.Y, u.Z);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        _downPos = e.GetPosition(this);
        _lastMouse = _downPos;
        _dragging = true;
        _dragButton = e.ChangedButton;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var p = e.GetPosition(this);
        var dx = (float)(p.X - _lastMouse.X);
        var dy = (float)(p.Y - _lastMouse.Y);
        _lastMouse = p;

        if (_dragButton == MouseButton.Left)
        {
            _orbit.Orbit(-dx * 0.01f, dy * 0.01f);
        }
        else
        {
            var scale = _orbit.Distance * 0.0015f;
            _orbit.Pan(-dx * scale, dy * scale);
        }

        ApplyCamera();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();

        var p = e.GetPosition(this);
        var moved = (p - _downPos).Length;
        if (e.ChangedButton == MouseButton.Left && moved < 4)
        {
            Pick(p);
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) =>
        ApplyZoom(e.Delta > 0 ? 0.9f : 1.0f / 0.9f);

    private void ApplyZoom(float factor)
    {
        _orbit.Zoom(factor);
        ApplyCamera();
    }

    private void Pick(Point p)
    {
        if (_scene.ActiveTool is not { ExpectedDatum: DatumKind.Point })
        {
            return;
        }

        Point3D? best = null;
        var bestDist = double.MaxValue;
        VisualTreeHelper.HitTest(
            View,
            null,
            result =>
            {
                if (result is RayMeshGeometry3DHitTestResult ray && ray.DistanceToRayOrigin < bestDist)
                {
                    bestDist = ray.DistanceToRayOrigin;
                    best = ray.PointHit;
                }

                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(p));

        if (best is { } hit)
        {
            _scene.AddPick(new Datum(DatumKind.Point, new Vector3((float)hit.X, (float)hit.Y, (float)hit.Z)));
        }
    }

    private static void AddModel(ModelVisual3D root, Geometry3D geometry, Material material, Material? back = null)
    {
        root.Children.Add(new ModelVisual3D
        {
            Content = new GeometryModel3D(geometry, material) { BackMaterial = back ?? material },
        });
    }

    private static Material Diffuse(Color color) => new DiffuseMaterial(new SolidColorBrush(color));

    private static Material Emissive(Color color) => new EmissiveMaterial(new SolidColorBrush(color));
}
