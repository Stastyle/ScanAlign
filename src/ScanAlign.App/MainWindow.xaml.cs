using System.Windows;
using ScanAlign.App.ViewModels;
using ScanAlign.App.Viewport;

namespace ScanAlign.App;

/// <summary>The four-zone shell: toolbar, tool rail, 3D viewport, inspector, status bar.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, SceneViewport viewport)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewportHost.Content = viewport;
    }
}
