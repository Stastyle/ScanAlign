using System.Reflection;
using ScanAlign.Core.Alignment;

namespace ScanAlign.Core.Registry;

/// <summary>
/// Discovers <see cref="IAlignmentTool"/> implementations for the tool rail. Alignment tracks
/// add a class; the rail picks it up automatically. Tools are ordered by their registered
/// <see cref="IAlignmentTool.Name"/> for a stable rail order.
/// </summary>
public sealed class AlignmentToolRegistry
{
    private readonly List<IAlignmentTool> _tools;

    /// <summary>Scan the given assemblies for alignment tools.</summary>
    public AlignmentToolRegistry(IEnumerable<Assembly> assemblies)
    {
        _tools = ReflectionDiscovery.Instantiate<IAlignmentTool>(assemblies.ToArray())
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Scan the Core assembly (where the built-in tools live).</summary>
    public AlignmentToolRegistry()
        : this(new[] { typeof(AlignmentToolRegistry).Assembly })
    {
    }

    public IReadOnlyList<IAlignmentTool> Tools => _tools;

    /// <summary>Look up a tool by its stable id, or null if not present.</summary>
    public IAlignmentTool? ById(string id) =>
        _tools.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
}
