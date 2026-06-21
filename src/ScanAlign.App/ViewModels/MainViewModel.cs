using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScanAlign.App.Services;
using ScanAlign.Core.Model;

namespace ScanAlign.App.ViewModels;

/// <summary>
/// The window's view-model: surfaces the tool rail, target controls, live proposal/inspector
/// readouts, and the toolbar commands, all driven by <see cref="ISceneService"/>.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ISceneService _scene;
    private readonly IDialogService _dialogs;

    private ToolItemViewModel? _selectedTool;
    private TargetOption _selectedTarget = null!;
    private OriginOption _selectedOrigin = null!;
    private bool _flip;
    private bool _onAxis;

    public MainViewModel(ISceneService scene, IDialogService dialogs)
    {
        _scene = scene;
        _dialogs = dialogs;

        Tools = new ObservableCollection<ToolItemViewModel>(scene.Tools.Select(t => new ToolItemViewModel(t)));

        OpenCommand = new AsyncRelayCommand(OpenAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => _scene.Current is not null);
        UndoCommand = new RelayCommand(_scene.Undo, () => _scene.CanUndo);
        RedoCommand = new RelayCommand(_scene.Redo, () => _scene.CanRedo);
        ResetCommand = new RelayCommand(_scene.ResetAlignment, () => _scene.CanUndo);
        CommitCommand = new RelayCommand(_scene.Commit, () => _scene.Proposal is { IsComplete: true });
        ClearPicksCommand = new RelayCommand(_scene.ClearPicks, () => _scene.Picks.Count > 0 || _scene.CurrentClusterSize > 0);
        RemoveLastPickCommand = new RelayCommand(_scene.RemoveLastPick, () => _scene.AllPickedPoints.Count > 0);
        NewCenterCommand = new RelayCommand(_scene.StartNewCenter, () => _scene.IsCentroidTool && _scene.CurrentClusterSize > 0);

        _scene.Changed += (_, _) => RefreshAll();

        _selectedTarget = TargetOptions[0];
        _selectedOrigin = OriginOptions[0];
        PushTarget();
    }

    public ObservableCollection<ToolItemViewModel> Tools { get; }

    public IReadOnlyList<TargetOption> TargetOptions { get; } = new[]
    {
        new TargetOption("Face → ground (XY, up +Z)", "Lay the picked face flat on the ground plane; its normal points up (+Z). The usual choice for Fusion.", TargetKind.PlaneXY),
        new TargetOption("Face → XZ (normal +Y)", "Make the picked face parallel to the XZ plane (normal along +Y).", TargetKind.PlaneXZ),
        new TargetOption("Face → YZ (normal +X)", "Make the picked face parallel to the YZ plane (normal along +X).", TargetKind.PlaneYZ),
        new TargetOption("Line → X axis", "Make the picked line / two holes parallel to the X axis.", TargetKind.AxisX),
        new TargetOption("Line → Y axis", "Make the picked line / two holes parallel to the Y axis.", TargetKind.AxisY),
        new TargetOption("Line → Z axis", "Make the picked line / two holes parallel to the Z axis.", TargetKind.AxisZ),
    };

    public IReadOnlyList<OriginOption> OriginOptions { get; } = new[]
    {
        new OriginOption("Move datum to origin", "Put the picked feature (the fitted plane/line point) at (0,0,0).", OriginPolicy.PlaneOrigin),
        new OriginOption("Keep position", "Only rotate — leave the part where it sits.", OriginPolicy.Keep),
        new OriginOption("Picked point to origin", "Put the last point you clicked at (0,0,0).", OriginPolicy.PickedPoint),
        new OriginOption("Center bounding box", "Move the part's bounding-box center to (0,0,0).", OriginPolicy.BBoxCenter),
    };

    public AsyncRelayCommand OpenCommand { get; }

    public AsyncRelayCommand SaveCommand { get; }

    public RelayCommand UndoCommand { get; }

    public RelayCommand RedoCommand { get; }

    public RelayCommand ResetCommand { get; }

    public RelayCommand CommitCommand { get; }

    public RelayCommand ClearPicksCommand { get; }

    public RelayCommand RemoveLastPickCommand { get; }

    public RelayCommand NewCenterCommand { get; }

    public ToolItemViewModel? SelectedTool
    {
        get => _selectedTool;
        set
        {
            if (SetProperty(ref _selectedTool, value))
            {
                _scene.SelectTool(value?.Id);
            }
        }
    }

    public TargetOption SelectedTarget
    {
        get => _selectedTarget;
        set
        {
            if (value is not null && SetProperty(ref _selectedTarget, value))
            {
                OnPropertyChanged(nameof(TargetDescription));
                PushTarget();
            }
        }
    }

    public OriginOption SelectedOrigin
    {
        get => _selectedOrigin;
        set
        {
            if (value is not null && SetProperty(ref _selectedOrigin, value))
            {
                OnPropertyChanged(nameof(OriginDescription));
                PushTarget();
            }
        }
    }

    public bool Flip
    {
        get => _flip;
        set
        {
            if (SetProperty(ref _flip, value))
            {
                PushTarget();
            }
        }
    }

    public bool OnAxis
    {
        get => _onAxis;
        set
        {
            if (SetProperty(ref _onAxis, value))
            {
                PushTarget();
            }
        }
    }

    public bool IsCentroidTool => _scene.IsCentroidTool;

    public string TargetDescription => _selectedTarget?.Description ?? string.Empty;

    public string OriginDescription => _selectedOrigin?.Description ?? string.Empty;

    public bool HasObject => _scene.Current is not null;

    public string PromptText
    {
        get
        {
            if (_scene.Current is null)
            {
                return "Open a scan (OBJ · PLY · STL) to begin.";
            }

            if (_scene.ActiveTool is null)
            {
                return "Pick an alignment tool on the left.";
            }

            return _scene.Proposal?.Explanation ?? "Click in the viewport to add datums.";
        }
    }

    public string PickInfo
    {
        get
        {
            if (_scene.ActiveTool is not { } tool)
            {
                return string.Empty;
            }

            if (_scene.IsCentroidTool)
            {
                return $"Centers {_scene.Picks.Count}/{tool.RequiredPicks} · current cluster {_scene.CurrentClusterSize} pts";
            }

            return $"Picks {_scene.Picks.Count}/{tool.RequiredPicks}";
        }
    }

    public string ResidualText =>
        _scene.Proposal is { IsComplete: true } p ? $"Residual {p.Residual:0.###}" : "—";

    public string TransformText
    {
        get
        {
            var m = _scene.Proposal is { IsComplete: true } p ? p.Transform : (_scene.Current?.World ?? Matrix4x4.Identity);
            return FormatTransform(m);
        }
    }

    public string BoundsText
    {
        get
        {
            if (_scene.WorldBounds is not { } b)
            {
                return "—";
            }

            var s = b.Size;
            return $"BBox {s.X:0.#} × {s.Y:0.#} × {s.Z:0.#}";
        }
    }

    public string TriangleText
    {
        get
        {
            if (_scene.Current is not { } o)
            {
                return string.Empty;
            }

            return o.Original.IsPointCloud
                ? $"{o.Original.Vertices.Count:N0} pts"
                : $"{o.Original.TriangleCount:N0} △";
        }
    }

    public string UnitText => _scene.Current is { } o ? o.Original.Unit.ToString().ToLowerInvariant() : string.Empty;

    // A fresh array each read: the underlying List grows in place, so returning the same reference
    // would leave the bound ItemsControl unaware that steps were added.
    public IReadOnlyList<AlignmentStep> History =>
        _scene.Current?.Stack.Steps.ToArray() ?? Array.Empty<AlignmentStep>();

    private void PushTarget() =>
        _scene.Target = new AlignmentTarget(
            _selectedTarget.Value, _selectedOrigin.Value, UpAxis.Z, _flip,
            _onAxis ? AxisPlacement.OnAxis : AxisPlacement.Parallel);

    private async Task OpenAsync()
    {
        if (_dialogs.OpenMesh() is not { } path)
        {
            return;
        }

        try
        {
            await _scene.LoadAsync(path);
        }
        catch (Exception ex)
        {
            _dialogs.Error($"Couldn't open the file:\n{ex.Message}");
        }
    }

    private async Task SaveAsync()
    {
        var suggested = _scene.Current?.Original.SourcePath is { } src
            ? Path.GetFileNameWithoutExtension(src) + "-aligned.ply"
            : "aligned.ply";

        if (_dialogs.SaveMesh(suggested) is not { } path)
        {
            return;
        }

        try
        {
            await _scene.ExportAsync(path);
        }
        catch (Exception ex)
        {
            _dialogs.Error($"Couldn't export the file:\n{ex.Message}");
        }
    }

    private void RefreshAll()
    {
        OnPropertyChanged(string.Empty); // refresh all bound readouts
        SaveCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        CommitCommand.NotifyCanExecuteChanged();
        ClearPicksCommand.NotifyCanExecuteChanged();
        RemoveLastPickCommand.NotifyCanExecuteChanged();
        NewCenterCommand.NotifyCanExecuteChanged();
    }

    private static string FormatTransform(Matrix4x4 m)
    {
        if (!Matrix4x4.Decompose(m, out _, out var rot, out var t))
        {
            return "(non-rigid)";
        }

        var (rx, ry, rz) = ToEulerDegrees(rot);
        var c = CultureInfo.InvariantCulture;
        return string.Format(c, "t  {0:0.##}  {1:0.##}  {2:0.##}\nr  {3:0.#}°  {4:0.#}°  {5:0.#}°", t.X, t.Y, t.Z, rx, ry, rz);
    }

    private static (float X, float Y, float Z) ToEulerDegrees(Quaternion q)
    {
        // ZYX intrinsic decomposition, returned as degrees.
        var sinrCosp = 2f * ((q.W * q.X) + (q.Y * q.Z));
        var cosrCosp = 1f - (2f * ((q.X * q.X) + (q.Y * q.Y)));
        var roll = MathF.Atan2(sinrCosp, cosrCosp);

        var sinp = 2f * ((q.W * q.Y) - (q.Z * q.X));
        var pitch = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

        var sinyCosp = 2f * ((q.W * q.Z) + (q.X * q.Y));
        var cosyCosp = 1f - (2f * ((q.Y * q.Y) + (q.Z * q.Z)));
        var yaw = MathF.Atan2(sinyCosp, cosyCosp);

        const float ToDeg = 180f / MathF.PI;
        return (roll * ToDeg, pitch * ToDeg, yaw * ToDeg);
    }
}
