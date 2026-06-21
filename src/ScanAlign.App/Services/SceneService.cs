using ScanAlign.Core.Alignment;
using ScanAlign.Core.IO;
using ScanAlign.Core.Model;
using ScanAlign.Core.Registry;
using ScanAlign.Core.Solvers;

namespace ScanAlign.App.Services;

/// <inheritdoc cref="ISceneService"/>
public sealed class SceneService : ISceneService
{
    private readonly MeshFormatRegistry _formats;
    private readonly IUnitDetector _unitDetector;
    private readonly IBoundingBox _boundingBox;
    private readonly ICircleFitter _circleFitter;

    // Picks are organized into groups. For point-mode tools each click is its own group; for
    // centroid tools clicks accumulate in the current group until the user starts a new center.
    private readonly List<List<Vector3>> _groups = new();
    private readonly Stack<AlignmentStep> _redo = new();

    private AlignmentTarget _target = AlignmentTarget.Default;

    public SceneService(
        MeshFormatRegistry formats,
        AlignmentToolRegistry tools,
        IUnitDetector unitDetector,
        IBoundingBox boundingBox,
        ICircleFitter circleFitter)
    {
        _formats = formats;
        _unitDetector = unitDetector;
        _boundingBox = boundingBox;
        _circleFitter = circleFitter;
        Tools = tools.Tools;
    }

    public SceneObject? Current { get; private set; }

    public IReadOnlyList<IAlignmentTool> Tools { get; }

    public IAlignmentTool? ActiveTool { get; private set; }

    public AlignmentTarget Target
    {
        get => _target;
        set
        {
            _target = value;
            Recompute();
        }
    }

    private bool CentroidMode => ActiveTool?.ExpectedDatum == DatumKind.Centroid;

    /// <summary>One datum per non-empty group: its estimated center, with the raw points attached.</summary>
    public IReadOnlyList<Datum> Picks => _groups
        .Where(g => g.Count > 0)
        .Select(g => new Datum(CentroidMode ? DatumKind.Centroid : DatumKind.Point, GroupCenter(g), SupportPoints: g.ToList()))
        .ToList();

    /// <summary>Every raw clicked point (across all groups), for rendering pick markers.</summary>
    public IReadOnlyList<Vector3> AllPickedPoints => _groups.SelectMany(g => g).ToList();

    /// <summary>The estimated center of each non-empty group, for rendering center markers.</summary>
    public IReadOnlyList<Vector3> Centroids => _groups.Where(g => g.Count > 0).Select(GroupCenter).ToList();

    /// <summary>True when the active tool clusters points into averaged centers.</summary>
    public bool IsCentroidTool => CentroidMode;

    /// <summary>Number of points in the current (last) group being built.</summary>
    public int CurrentClusterSize => _groups.Count > 0 ? _groups[^1].Count : 0;

    public AlignmentProposal? Proposal { get; private set; }

    public bool CanUndo => Current is { Stack.IsEmpty: false };

    public bool CanRedo => _redo.Count > 0;

    public Aabb? WorldBounds =>
        Current is null ? null : _boundingBox.Compute(Current.ToWorld().Vertices);

    public event EventHandler? Changed;

    public async Task LoadAsync(string path, CancellationToken ct = default)
    {
        var reader = _formats.ReaderFor(path)
            ?? throw new NotSupportedException($"No reader for '{Path.GetExtension(path)}'.");

        var mesh = await Task.Run(() =>
        {
            using var fs = File.OpenRead(path);
            return reader.Read(fs, ReadOptions.Default);
        }, ct);

        if (mesh.Unit == Unit.Unknown)
        {
            mesh = mesh with { Unit = _unitDetector.Detect(mesh), SourcePath = path };
        }
        else
        {
            mesh = mesh with { SourcePath = path };
        }

        Current = new SceneObject(mesh);
        _groups.Clear();
        _redo.Clear();
        Recompute();
    }

    public async Task ExportAsync(string path, CancellationToken ct = default)
    {
        if (Current is null)
        {
            throw new InvalidOperationException("Nothing to export.");
        }

        var writer = _formats.WriterFor(path)
            ?? throw new NotSupportedException($"No writer for '{Path.GetExtension(path)}'.");

        var baked = Current.ToWorld();
        var provenance = BuildProvenance(Current);
        await Task.Run(() =>
        {
            using var fs = File.Create(path);
            writer.Write(fs, baked, WriteOptions.Default with { ProvenanceHeader = provenance });
        }, ct);
    }

    public void SelectTool(string? id)
    {
        ActiveTool = id is null ? null : Tools.FirstOrDefault(t => t.Id == id);
        _groups.Clear();
        Recompute();
    }

    public void AddPick(Datum datum)
    {
        var p = datum.Position;
        if (CentroidMode)
        {
            // Accumulate into the current center cluster.
            if (_groups.Count == 0)
            {
                _groups.Add(new List<Vector3>());
            }

            _groups[^1].Add(p);
        }
        else
        {
            // Each click is its own datum.
            _groups.Add(new List<Vector3> { p });
        }

        Recompute();
    }

    /// <summary>Finalize the current center cluster and begin a new one (centroid tools only).</summary>
    public void StartNewCenter()
    {
        if (CentroidMode && _groups.Count > 0 && _groups[^1].Count > 0)
        {
            _groups.Add(new List<Vector3>());
            Recompute();
        }
    }

    public void RemoveLastPick()
    {
        // Drop the most recent raw point; clean up an emptied trailing group.
        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            if (_groups[i].Count > 0)
            {
                _groups[i].RemoveAt(_groups[i].Count - 1);
                if (_groups[i].Count == 0 && i == _groups.Count - 1)
                {
                    _groups.RemoveAt(i);
                }

                Recompute();
                return;
            }
        }
    }

    public void ClearPicks()
    {
        if (_groups.Count > 0)
        {
            _groups.Clear();
            Recompute();
        }
    }

    public void Commit()
    {
        if (Current is null || Proposal is not { IsComplete: true } proposal)
        {
            return;
        }

        Current.Stack.Push(AlignmentStep.Create(proposal.Transform, proposal.Explanation, proposal.Residual));
        _redo.Clear();
        _groups.Clear();
        Recompute();
    }

    public void Undo()
    {
        if (Current is { Stack.IsEmpty: false } && Current.Stack.Steps.Count > 0)
        {
            var last = Current.Stack.Steps[^1];
            Current.Stack.Pop();
            _redo.Push(last);
            Recompute();
        }
    }

    public void Redo()
    {
        if (Current is not null && _redo.Count > 0)
        {
            Current.Stack.Push(_redo.Pop());
            Recompute();
        }
    }

    public void ResetAlignment()
    {
        if (Current is { Stack.IsEmpty: false })
        {
            Current.Stack.Clear();
            _redo.Clear();
            Recompute();
        }
    }

    private void Recompute()
    {
        Proposal = ComputeProposal();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private AlignmentProposal? ComputeProposal()
    {
        if (Current is null || ActiveTool is null)
        {
            return null;
        }

        // Whole-mesh tools (PCA auto, drop-to-floor) need the current world point cloud as input.
        if (ActiveTool.Id is "pca-auto" or "drop-to-floor")
        {
            var world = Current.ToWorld().Vertices;
            var datum = new Datum(DatumKind.PlaneRegion, Vector3.Zero, SupportPoints: world);
            return ActiveTool.Solve(new[] { datum }, _target);
        }

        return ActiveTool.Solve(Picks, _target);
    }

    /// <summary>
    /// The center a pick group represents. For centroid tools this is a least-squares circle center —
    /// so clustering many clicks in one spot doesn't bias it (points on the same circle all agree), and
    /// it converges on the true center as boundary points are added. Falls back to the plain average for
    /// point tools or when a circle can't be fit reliably (too few points, collinear, or wildly off).
    /// </summary>
    private Vector3 GroupCenter(IReadOnlyList<Vector3> group)
    {
        if (!CentroidMode || group.Count < 3)
        {
            return Average(group);
        }

        var avg = Average(group);
        try
        {
            var fit = _circleFitter.Fit(group);
            var spread = group.Max(p => Vector3.Distance(p, avg));
            var sane = spread > 1e-6f
                && Vector3.Distance(fit.Circle.Center, avg) <= 5f * spread
                && fit.Circle.Radius <= 50f * spread
                && !float.IsNaN(fit.Circle.Center.X);
            return sane ? fit.Circle.Center : avg;
        }
        catch (ArgumentException)
        {
            return avg;
        }
    }

    private static Vector3 Average(IReadOnlyList<Vector3> pts)
    {
        var sum = Vector3.Zero;
        foreach (var p in pts)
        {
            sum += p;
        }

        return sum / pts.Count;
    }

    private static string BuildProvenance(SceneObject scene)
    {
        var sb = new StringBuilder();
        sb.Append("ScanAlign alignment");
        if (scene.Original.SourcePath is { } src)
        {
            sb.Append(" of ").Append(Path.GetFileName(src));
        }

        foreach (var step in scene.Stack.Steps)
        {
            sb.Append('\n').Append(step.Description);
        }

        return sb.ToString();
    }
}
