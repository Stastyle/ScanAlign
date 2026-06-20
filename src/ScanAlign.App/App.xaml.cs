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

        // Core (discovered by reflection over the Core assembly).
        services.AddSingleton<MeshFormatRegistry>();
        services.AddSingleton<AlignmentToolRegistry>();
        services.AddSingleton<IUnitDetector, UnitDetector>();
        services.AddSingleton<IBoundingBox, BoundingBox>();

        // App.
        services.AddSingleton<ISceneService, SceneService>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SceneViewport>();
        services.AddSingleton<MainWindow>();

        _services = services.BuildServiceProvider();
        _services.GetRequiredService<MainWindow>().Show();
    }
}
