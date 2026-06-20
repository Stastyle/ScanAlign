namespace ScanAlign.Core.Measure;

/// <summary>Distance between two points, with per-axis components (<paramref name="Delta"/>).</summary>
public sealed record DistanceResult(float Distance, Vector3 Delta);

/// <summary>Angle between two datums, in degrees.</summary>
public sealed record AngleResult(float Degrees);

/// <summary>A fitted hole/circle diameter and the rim fit RMS.</summary>
public sealed record DiameterResult(float Diameter, double Rms);
