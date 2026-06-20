namespace ScanAlign.Core.Model;

/// <summary>An infinite plane defined by a point on it and its unit normal.</summary>
public readonly record struct Plane(Vector3 Point, Vector3 Normal);

/// <summary>An infinite line defined by a point on it and its unit direction.</summary>
public readonly record struct Line3(Vector3 Point, Vector3 Direction);

/// <summary>A circle in 3D: center, plane normal, and radius (e.g. a fitted hole rim).</summary>
public readonly record struct Circle3(Vector3 Center, Vector3 Normal, float Radius);

/// <summary>An axis-aligned bounding box.</summary>
public readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;
    public Vector3 Center => (Min + Max) * 0.5f;
}
