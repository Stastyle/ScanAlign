using Microsoft.Extensions.DependencyInjection;
using ScanAlign.App;
using ScanAlign.App.Services;
using ScanAlign.Core.Registry;

namespace ScanAlign.Tests.App;

/// <summary>
/// Verifies the real DI composition resolves working registries — guards against the container
/// picking a registry's assembly-scanning constructor's greedier sibling and discovering nothing.
/// </summary>
public class CompositionTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        global::ScanAlign.App.App.ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void MeshFormatRegistry_resolved_from_di_finds_all_formats()
    {
        using var provider = BuildProvider();
        var formats = provider.GetRequiredService<MeshFormatRegistry>();

        Assert.NotNull(formats.ReaderFor(".obj"));
        Assert.NotNull(formats.ReaderFor(".ply"));
        Assert.NotNull(formats.ReaderFor(".stl"));
        Assert.NotNull(formats.WriterFor(".obj"));
        Assert.NotNull(formats.WriterFor(".stl"));
    }

    [Fact]
    public void AlignmentToolRegistry_resolved_from_di_has_tools()
    {
        using var provider = BuildProvider();
        var tools = provider.GetRequiredService<AlignmentToolRegistry>();

        Assert.NotEmpty(tools.Tools);
        Assert.NotNull(tools.ById("three-point-plane"));
    }

    [Fact]
    public void SceneService_resolves_with_its_dependencies()
    {
        using var provider = BuildProvider();
        var scene = provider.GetRequiredService<ISceneService>();

        Assert.NotEmpty(scene.Tools);
    }
}
