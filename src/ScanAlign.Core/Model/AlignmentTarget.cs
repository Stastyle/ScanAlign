namespace ScanAlign.Core.Model;

/// <summary>Which world axis or plane a datum should be mapped onto.</summary>
public enum TargetKind
{
    AxisX,
    AxisY,
    AxisZ,
    PlaneXY,
    PlaneXZ,
    PlaneYZ,
}

/// <summary>Where the origin lands after an alignment step.</summary>
public enum OriginPolicy
{
    /// <summary>Leave translation unchanged (rotation only).</summary>
    Keep,

    /// <summary>Move the bounding-box center to the world origin.</summary>
    BBoxCenter,

    /// <summary>Move the picked point to the world origin.</summary>
    PickedPoint,

    /// <summary>Move the fitted plane's reference point to the world origin.</summary>
    PlaneOrigin,
}

/// <summary>World up-axis convention. Z-up is the Fusion 360 default.</summary>
public enum UpAxis
{
    Z,
    Y,
}

/// <summary>
/// Whether the aligned element only matches the target's orientation or is also moved onto it.
/// </summary>
public enum AxisPlacement
{
    /// <summary>Match orientation only — the element stays at its current offset (just parallel).</summary>
    Parallel,

    /// <summary>Lie on the target: a line sits on the axis; a face lies in the plane (coincident).</summary>
    OnAxis,
}

/// <summary>
/// The target an alignment tool maps its datums onto. <paramref name="FlipX"/>/<paramref name="FlipY"/>/
/// <paramref name="FlipZ"/> add a 180° turn about that world axis after alignment, to correct a part
/// that came out facing the wrong way (any axis, independently). <paramref name="Placement"/> chooses
/// whether the element is only made parallel to the target or actually placed on it.
/// </summary>
public sealed record AlignmentTarget(
    TargetKind Kind,
    OriginPolicy Origin,
    UpAxis Up,
    bool FlipX = false,
    bool FlipY = false,
    bool FlipZ = false,
    AxisPlacement Placement = AxisPlacement.Parallel)
{
    /// <summary>Default target: snap a face to XY (normal to +Z), Z-up, origin unchanged.</summary>
    public static AlignmentTarget Default { get; } =
        new(TargetKind.PlaneXY, OriginPolicy.Keep, UpAxis.Z);
}
