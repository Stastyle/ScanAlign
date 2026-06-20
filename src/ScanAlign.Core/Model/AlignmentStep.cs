namespace ScanAlign.Core.Model;

/// <summary>
/// One committed entry in the non-destructive alignment history. Carries the rigid transform,
/// a description (the recipe line), the fit residual, and when it was applied.
/// </summary>
public sealed record AlignmentStep(
    Matrix4x4 Transform,
    string Description,
    double Residual,
    DateTimeOffset At)
{
    public static AlignmentStep Create(Matrix4x4 transform, string description, double residual) =>
        new(transform, description, residual, DateTimeOffset.UtcNow);
}
