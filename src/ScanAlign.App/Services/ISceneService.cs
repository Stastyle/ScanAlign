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

    void RemoveLastPick();

    void ClearPicks();

    /// <summary>Commit the current complete proposal as a new alignment step.</summary>
    void Commit();

    void Undo();

    void Redo();

    /// <summary>Drop all alignment steps (reset to the imported pose).</summary>
    void ResetAlignment();
}
