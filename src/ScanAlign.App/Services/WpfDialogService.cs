using System.Windows;
using Microsoft.Win32;

namespace ScanAlign.App.Services;

/// <inheritdoc cref="IDialogService"/>
public sealed class WpfDialogService : IDialogService
{
    private const string MeshFilter =
        "3D scans (*.obj;*.ply;*.stl)|*.obj;*.ply;*.stl|" +
        "Wavefront OBJ (*.obj)|*.obj|" +
        "Stanford PLY (*.ply)|*.ply|" +
        "STL (*.stl)|*.stl|" +
        "All files (*.*)|*.*";

    public string? OpenMesh()
    {
        var dlg = new OpenFileDialog { Title = "Open scan", Filter = MeshFilter, CheckFileExists = true };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? SaveMesh(string suggestedName)
    {
        var dlg = new SaveFileDialog { Title = "Export aligned scan", Filter = MeshFilter, FileName = suggestedName };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public void Error(string message) =>
        MessageBox.Show(message, "ScanAlign", MessageBoxButton.OK, MessageBoxImage.Warning);
}
