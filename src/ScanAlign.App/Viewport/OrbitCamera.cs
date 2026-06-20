namespace ScanAlign.App.Viewport;

/// <summary>
/// A Z-up orbit camera model (pure math, no WPF types — so it's unit-testable). Holds a target,
/// orbit distance, azimuth (yaw) and elevation (pitch), and derives the eye position, look and up
/// vectors a <c>PerspectiveCamera</c> consumes.
/// </summary>
public sealed class OrbitCamera
{
    private const float MaxPitch = 1.5533f; // ~89°
    private float _yaw = 0.7f;
    private float _pitch = 0.6f;
    private float _distance = 5f;

    public Vector3 Target { get; set; } = Vector3.Zero;

    public float Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, 1e-3f, 1e7f);
    }

    public float Yaw
    {
        get => _yaw;
        set => _yaw = value;
    }

    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
    }

    /// <summary>Unit vector from target toward the eye.</summary>
    public Vector3 Direction => new(
        MathF.Cos(_pitch) * MathF.Cos(_yaw),
        MathF.Cos(_pitch) * MathF.Sin(_yaw),
        MathF.Sin(_pitch));

    public Vector3 Position => Target + (Direction * _distance);

    public Vector3 LookDirection => -Direction;

    public Vector3 Up => Vector3.UnitZ;

    /// <summary>Camera right vector in the ground plane (for screen-space panning).</summary>
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(LookDirection, Vector3.UnitZ));

    public void Orbit(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch += deltaPitch;
    }

    /// <summary>Multiplicative zoom; factor &lt; 1 moves closer, &gt; 1 moves farther.</summary>
    public void Zoom(float factor) => Distance *= factor;

    /// <summary>Pan the target in the screen plane by the given right/up amounts (world units).</summary>
    public void Pan(float right, float up)
    {
        var upVec = Vector3.Normalize(Vector3.Cross(Right, LookDirection));
        Target += (Right * right) + (upVec * up);
    }

    /// <summary>Frame a bounding sphere: center the target and back off to fit the radius.</summary>
    public void FrameExtents(Vector3 center, float radius)
    {
        Target = center;
        Distance = MathF.Max(radius, 1e-3f) * 2.5f;
    }
}
