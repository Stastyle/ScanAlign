using ScanAlign.Core.Model;

namespace ScanAlign.Core.Solvers;

/// <summary>A fitted plane and the RMS distance of the input points to it (in mesh units).</summary>
public sealed record PlaneFitResult(Plane Plane, double Rms);

/// <summary>A fitted line and the RMS distance of the input points to it.</summary>
public sealed record LineFitResult(Line3 Line, double Rms);

/// <summary>A fitted circle and the RMS fit error of the rim points.</summary>
public sealed record CircleFitResult(Circle3 Circle, double Rms);
