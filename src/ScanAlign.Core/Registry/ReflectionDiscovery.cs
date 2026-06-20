using System.Reflection;

namespace ScanAlign.Core.Registry;

/// <summary>
/// Shared assembly-scanning helper. Registries use this to discover concrete implementations
/// of a contract interface and instantiate them via their parameterless constructor. This is
/// what lets new readers/writers/tools register by simply existing — no central edits.
/// </summary>
public static class ReflectionDiscovery
{
    /// <summary>Instantiate every concrete, public, parameterless-constructible <typeparamref name="T"/>.</summary>
    public static IReadOnlyList<T> Instantiate<T>(IEnumerable<Assembly> assemblies)
        where T : class
    {
        var result = new List<T>();
        foreach (var type in assemblies.SelectMany(SafeGetTypes).Distinct())
        {
            if (!typeof(T).IsAssignableFrom(type))
            {
                continue;
            }

            if (type is { IsClass: true, IsAbstract: false } && type.GetConstructor(Type.EmptyTypes) is not null)
            {
                result.Add((T)Activator.CreateInstance(type)!);
            }
        }

        return result;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
