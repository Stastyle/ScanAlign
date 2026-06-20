using ScanAlign.Core.Model;

namespace ScanAlign.Core.IO;

/// <summary>
/// Heuristically suggests the length unit of a mesh (e.g. from bounding-box magnitude).
/// This only suggests — the app never silently rescales; it confirms with the user.
/// Returns <see cref="Unit.Unknown"/> when the guess is ambiguous.
/// </summary>
public interface IUnitDetector
{
    Unit Detect(MeshData mesh);
}
