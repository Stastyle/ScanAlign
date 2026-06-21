using ScanAlign.Core.Alignment;
using ScanAlign.Core.Model;

namespace ScanAlign.App.Services;

/// <summary>
/// The orchestration core: owns the loaded object and drives the alignment loop
/// (load → select tool → collect picks → live proposal → commit → undo/redo/reset → export).
/// UI-framework agnostic so it can be exercised headlessly. Raises <see cref="Changed"/> whenever
/// any observable state changes.
/// </summary>
public interface ISceneService
{
    SceneObject? Current { get; }

    IReadOnlyList<IAlignmentTool> Tools { get; }

    IAlignmentTool? ActiveTool { get; }

    AlignmentTarget Target { get; set; }

    IReadOnlyList<Datum> Picks { get; }

    /// <summary>Every raw clicked point, for rendering pick markers.</summary>
    IReadOnlyList<Vector3> AllPickedPoints { get; }

    /// <summary>The averaged center of each pick group, for rendering center markers.</summary>
    IReadOnlyList<Vector3> Centroids { get; }

    /// <summary>True when the active tool averages clusters of points into centers.</summary>
    bool IsCentroidTool { get; }

    /// <summary>Points in the current cluster being built (centroid tools).</summary>
    int CurrentClusterSize { get; }

    /// <summary>The live proposal for the current tool/picks/target, or null when not applicable.</summary>
    AlignmentProposal? Proposal { get; }

    bool CanUndo { get; }

    bool CanRedo { get; }

    /// <summary>World-space bounding box of the current object (post-alignment), or null if none loaded.</summary>
    Aabb? WorldBounds { get; }

    event EventHandler? Changed;

    Task LoadAsync(string path, CancellationToken ct = default);

    Task ExportAsync(string path, CancellationToken ct = default);

    void SelectTool(string? id);

    void AddPick(Datum datum);

    /// <summary>Finalize the current center cluster and start a new one (centroid tools).</summary>
    void StartNewCenter();

    void RemoveLastPick();

    void ClearPicks();

    /// <summary>Commit the current complete proposal as a new alignment step.</summary>
    void Commit();

    void Undo();

    void Redo();

    /// <summary>Drop all alignment steps (reset to the imported pose).</summary>
    void ResetAlignment();
}
