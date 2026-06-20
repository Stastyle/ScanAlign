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

    private readonly List<Datum> _picks = new();
    private readonly Stack<AlignmentStep> _redo = new();

    private AlignmentTarget _target = AlignmentTarget.Default;

    public SceneService(
        MeshFormatRegistry formats,
        AlignmentToolRegistry tools,
        IUnitDetector unitDetector,
        IBoundingBox boundingBox)
    {
        _formats = formats;
        _unitDetector = unitDetector;
        _boundingBox = boundingBox;
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

    public IReadOnlyList<Datum> Picks => _picks;

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
        _picks.Clear();
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
        _picks.Clear();
        Recompute();
    }

    public void AddPick(Datum datum)
    {
        _picks.Add(datum);
        Recompute();
    }

    public void RemoveLastPick()
    {
        if (_picks.Count > 0)
        {
            _picks.RemoveAt(_picks.Count - 1);
            Recompute();
        }
    }

    public void ClearPicks()
    {
        if (_picks.Count > 0)
        {
            _picks.Clear();
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
        _picks.Clear();
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

        // The whole-mesh tool (PCA auto) needs the current world point cloud as its support set.
        if (ActiveTool.Id == "pca-auto")
        {
            var world = Current.ToWorld().Vertices;
            var datum = new Datum(DatumKind.PlaneRegion, Vector3.Zero, SupportPoints: world);
            return ActiveTool.Solve(new[] { datum }, _target);
        }

        return ActiveTool.Solve(_picks, _target);
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
