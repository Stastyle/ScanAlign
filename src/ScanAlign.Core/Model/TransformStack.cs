namespace ScanAlign.Core.Model;

/// <summary>
/// The non-destructive alignment history for one object. Each committed alignment is one
/// <see cref="AlignmentStep"/>; the live world matrix is the product of all steps in push order.
/// Undo = <see cref="Pop"/>; "reset to imported" = <see cref="Clear"/>.
/// </summary>
/// <remarks>
/// Composition uses the row-vector convention that <see cref="System.Numerics"/> follows
/// (a point is transformed as <c>Vector3.Transform(p, Composite)</c>). Applying step 1 then
/// step 2 yields <c>Composite = T1 * T2</c>, so we fold left-to-right in push order.
/// </remarks>
public sealed class TransformStack
{
    private readonly List<AlignmentStep> _steps = new();
    private Matrix4x4 _composite = Matrix4x4.Identity;

    /// <summary>The committed steps, oldest first.</summary>
    public IReadOnlyList<AlignmentStep> Steps => _steps;

    /// <summary>The product of all steps (identity when empty).</summary>
    public Matrix4x4 Composite => _composite;

    /// <summary>True when no alignment has been committed (object is at its imported pose).</summary>
    public bool IsEmpty => _steps.Count == 0;

    /// <summary>Raised whenever the stack changes (push/pop/clear).</summary>
    public event EventHandler? Changed;

    /// <summary>Append a committed alignment step.</summary>
    public void Push(AlignmentStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
        Recompute();
    }

    /// <summary>Remove the most recent step (undo). No-op when empty.</summary>
    public void Pop()
    {
        if (_steps.Count == 0)
        {
            return;
        }

        _steps.RemoveAt(_steps.Count - 1);
        Recompute();
    }

    /// <summary>Remove a specific step by index (e.g. deleting one entry from history).</summary>
    public void RemoveAt(int index)
    {
        _steps.RemoveAt(index);
        Recompute();
    }

    /// <summary>Drop all steps (reset to the imported pose). No-op when already empty.</summary>
    public void Clear()
    {
        if (_steps.Count == 0)
        {
            return;
        }

        _steps.Clear();
        Recompute();
    }

    private void Recompute()
    {
        var m = Matrix4x4.Identity;
        foreach (var step in _steps)
        {
            m *= step.Transform;
        }

        _composite = m;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
