using ScanAlign.Core.Model;
using ScanAlign.Core.Solvers;

namespace ScanAlign.Core.Measure;

/// <summary>Read-only measurement queries used by the viewport overlays and the status bar.</summary>
public sealed class Measurements : IMeasurements
{
    private readonly ICircleFitter _circleFitter;
    private readonly IBoundingBox _boundingBox;

    public Measurements()
        : this(new CircleFitter(), new BoundingBox())
    {
    }

    public Measurements(ICircleFitter circleFitter, IBoundingBox boundingBox)
    {
        _circleFitter = circleFitter;
        _boundingBox = boundingBox;
    }

    public DistanceResult Distance(Vector3 a, Vector3 b)
    {
        var delta = b - a;
        return new DistanceResult(delta.Length(), delta);
    }

    public AngleResult Angle(Line3 a, Line3 b)
    {
        var da = Vector3.Normalize(a.Direction);
        var db = Vector3.Normalize(b.Direction);
        var cos = Math.Clamp(Vector3.Dot(da, db), -1f, 1f);
        return new AngleResult(MathF.Acos(cos) * (180f / MathF.PI));
    }

    public DiameterResult Diameter(IReadOnlyList<Vector3> rimPoints)
    {
        var fit = _circleFitter.Fit(rimPoints);
        return new DiameterResult(fit.Circle.Radius * 2f, fit.Rms);
    }

    public Aabb BoundingBox(IReadOnlyList<Vector3> points) => _boundingBox.Compute(points);
}
