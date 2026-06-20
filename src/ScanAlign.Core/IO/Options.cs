namespace ScanAlign.Core.IO;

/// <summary>Options for reading a mesh (progress reporting for large files).</summary>
public sealed record ReadOptions(IProgress<float>? Progress = null)
{
    public static ReadOptions Default { get; } = new();
}

/// <summary>
/// Options for writing a mesh. <paramref name="Binary"/> selects binary vs ASCII where the
/// format supports both; <paramref name="ProvenanceHeader"/> is an optional comment block
/// (the alignment recipe) embedded where the format allows.
/// </summary>
public sealed record WriteOptions(
    IProgress<float>? Progress = null,
    bool Binary = true,
    string? ProvenanceHeader = null)
{
    public static WriteOptions Default { get; } = new();
}
