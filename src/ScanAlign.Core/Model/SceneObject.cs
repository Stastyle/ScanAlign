namespace ScanAlign.Core.Model;

/// <summary>
/// One loaded object in the scene: the immutable imported <see cref="MeshData"/> plus its
/// alignment history. The applied pose is <see cref="World"/> = the transform stack's product.
/// </summary>
public sealed class SceneObject
{
    public SceneObject(MeshData original)
    {
        Original = original ?? throw new ArgumentNullException(nameof(original));
        Stack = new TransformStack();
    }

    /// <summary>The imported geometry, never mutated.</summary>
    public MeshData Original { get; }

    /// <summary>The non-destructive alignment history.</summary>
    public TransformStack Stack { get; }

    /// <summary>The current world transform (identity until an alignment is committed).</summary>
    public Matrix4x4 World => Stack.Composite;

    /// <summary>
    /// Produce the baked, world-aligned geometry for export. Vertices and normals are
    /// transformed by <see cref="World"/>; indices/colors/unit are preserved.
    /// </summary>
    public MeshData ToWorld()
    {
        var m = World;
        if (m == Matrix4x4.Identity)
        {
            return Original;
        }

        var verts = new Vector3[Original.Vertices.Count];
        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = Vector3.Transform(Original.Vertices[i], m);
        }

        Vector3[]? normals = null;
        if (Original.Normals is { Count: > 0 } srcNormals)
        {
            var normalMatrix = Matrix4x4.Identity;
            Matrix4x4.Invert(m, out var inv);
            normalMatrix = Matrix4x4.Transpose(inv);

            normals = new Vector3[srcNormals.Count];
            for (var i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.Normalize(Vector3.TransformNormal(srcNormals[i], normalMatrix));
            }
        }

        return Original with { Vertices = verts, Normals = normals };
    }
}
