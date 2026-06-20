namespace ScanAlign.Core.Model;

/// <summary>
/// The result a tool returns for the current set of picks: the rigid transform to apply,
/// a residual (fit error, in mesh units), a human-readable explanation, and whether enough
/// datums have been supplied yet (<see cref="IsComplete"/>).
/// </summary>
public sealed record AlignmentProposal(
    Matrix4x4 Transform,
    double Residual,
    string Explanation,
    bool IsComplete)
{
    /// <summary>An identity, not-yet-complete proposal (e.g. before enough picks are made).</summary>
    public static AlignmentProposal Pending(string explanation) =>
        new(Matrix4x4.Identity, 0.0, explanation, false);
}
