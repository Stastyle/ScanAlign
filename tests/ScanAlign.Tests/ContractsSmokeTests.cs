using ScanAlign.Core.Model;

namespace ScanAlign.Tests;

/// <summary>
/// Wave 0 gate: the contracts compile, the core model math behaves, and fixtures are deployed.
/// </summary>
public class ContractsSmokeTests
{
    [Fact]
    public void TransformStack_composes_steps_in_push_order()
    {
        var stack = new TransformStack();
        Assert.True(stack.IsEmpty);
        Assert.Equal(Matrix4x4.Identity, stack.Composite);

        var t1 = Matrix4x4.CreateTranslation(1, 0, 0);
        var t2 = Matrix4x4.CreateRotationZ(MathF.PI / 2f);
        stack.Push(AlignmentStep.Create(t1, "translate", 0));
        stack.Push(AlignmentStep.Create(t2, "rotate", 0));

        // Row-vector convention: applying t1 then t2 == multiply t1 * t2.
        Assert.Equal(t1 * t2, stack.Composite);
        Assert.Equal(2, stack.Steps.Count);

        stack.Pop();
        Assert.Equal(t1, stack.Composite);

        stack.Clear();
        Assert.True(stack.IsEmpty);
        Assert.Equal(Matrix4x4.Identity, stack.Composite);
    }

    [Fact]
    public void SceneObject_bakes_world_transform_into_vertices()
    {
        var mesh = new MeshData(
            new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0) },
            new[] { 0, 1, 0 },
            Normals: null,
            Colors: null,
            Unit.Millimeter,
            SourcePath: null);

        var scene = new SceneObject(mesh);
        Assert.Equal(Matrix4x4.Identity, scene.World);
        Assert.Same(mesh, scene.ToWorld()); // identity => same instance, no copy

        scene.Stack.Push(AlignmentStep.Create(Matrix4x4.CreateTranslation(10, 0, 0), "shift", 0));
        var world = scene.ToWorld();

        Assert.Equal(new Vector3(10, 0, 0), world.Vertices[0]);
        Assert.Equal(new Vector3(11, 0, 0), world.Vertices[1]);
        Assert.Equal(mesh.Indices, world.Indices); // topology preserved
        Assert.Equal(new Vector3(0, 0, 0), mesh.Vertices[0]); // original untouched
    }

    [Fact]
    public void AlignmentProposal_pending_is_identity_and_incomplete()
    {
        var p = AlignmentProposal.Pending("need 3 points");
        Assert.False(p.IsComplete);
        Assert.Equal(Matrix4x4.Identity, p.Transform);
    }

    [Fact]
    public void Fixtures_are_copied_to_output()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "cube.obj");
        Assert.True(File.Exists(path), $"Expected fixture at {path}");
    }
}
