using ScanAlign.Core.Model;

namespace ScanAlign.Core.Alignment;

/// <summary>
/// An alignment strategy. Implementations are discovered by reflection via
/// <c>AlignmentToolRegistry</c> and surfaced in the tool rail — adding a tool is just adding a
/// class. Given the user's picks and a target axis/plane, a tool returns the rigid transform
/// that snaps the geometry into place.
/// </summary>
public interface IAlignmentTool
{
    /// <summary>Stable identifier (e.g. "three-point-plane"). Used as a key, not shown to users.</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in the tool rail.</summary>
    string Name { get; }

    /// <summary>How many datums the tool needs before it can produce a complete proposal.</summary>
    int RequiredPicks { get; }

    /// <summary>The kind of datum the tool collects (drives the picking mode + prompt).</summary>
    DatumKind ExpectedDatum { get; }

    /// <summary>
    /// Compute a proposal for the current picks. With fewer than <see cref="RequiredPicks"/>
    /// datums, return <see cref="AlignmentProposal.Pending"/>.
    /// </summary>
    AlignmentProposal Solve(IReadOnlyList<Datum> picks, AlignmentTarget target);
}
