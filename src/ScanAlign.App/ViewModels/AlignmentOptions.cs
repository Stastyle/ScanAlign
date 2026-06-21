using ScanAlign.Core.Model;

namespace ScanAlign.App.ViewModels;

/// <summary>A human-readable choice for the "Snap to" target dropdown.</summary>
public sealed record TargetOption(string Label, string Description, TargetKind Value)
{
    public override string ToString() => Label;
}

/// <summary>A human-readable choice for the origin-placement dropdown.</summary>
public sealed record OriginOption(string Label, string Description, OriginPolicy Value)
{
    public override string ToString() => Label;
}
