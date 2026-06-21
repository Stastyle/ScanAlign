using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ScanAlign.App.Services;
using ScanAlign.App.ViewModels;
using ScanAlign.App.Viewport;
using ScanAlign.Core.IO;
using ScanAlign.Core.Registry;
using ScanAlign.Core.Solvers;

namespace ScanAlign.App;

/// <summary>
/// Composition root. Wires the Core registries/solvers and the App services/view-models, then shows
/// the main window. Registries discover formats and tools by reflection — adding a class is enough.
/// </summary>
public partial class App : Application
{
    private IServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();
        _services.GetRequiredService<MainWindow>().Show();

        // Optional: open a file passed on the command line (drag-onto-exe / "open with").
        if (e.Args.Length > 0 && File.Exists(e.Args[0]))
        {
            var scene = _services.GetRequiredService<ISceneService>();
            _ = LoadInitialAsync(scene, e.Args[0]);
        }
    }

    /// <summary>
    /// Registers all services. Note the registries use explicit factories so the container invokes
    /// their parameterless (assembly-scanning) constructor — otherwise DI would pick the greedier
    /// <c>IEnumerable&lt;Assembly&gt;</c> constructor and resolve it to an empty list, discovering nothing.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Core (discovered by reflection over the Core assembly).
        services.AddSingleton(_ => new MeshFormatRegistry());
        services.AddSingleton(_ => new AlignmentToolRegistry());
        services.AddSingleton<IUnitDetector, UnitDetector>();
        services.AddSingleton<IBoundingBox, BoundingBox>();
        services.AddSingleton<ICircleFitter, CircleFitter>();

        // App.
        services.AddSingleton<ISceneService, SceneService>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SceneViewport>();
        services.AddSingleton<MainWindow>();
    }

    private static async Task LoadInitialAsync(ISceneService scene, string path)
    {
        try
        {
            await scene.LoadAsync(path);
        }
        catch
        {
            // A bad path on startup shouldn't crash the app; the user can open another file.
        }
    }
}
