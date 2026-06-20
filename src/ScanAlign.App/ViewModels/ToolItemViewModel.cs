using CommunityToolkit.Mvvm.ComponentModel;
using ScanAlign.Core.Alignment;

namespace ScanAlign.App.ViewModels;

/// <summary>A row in the tool rail, wrapping one discovered <see cref="IAlignmentTool"/>.</summary>
public sealed class ToolItemViewModel : ObservableObject
{
    public ToolItemViewModel(IAlignmentTool tool) => Tool = tool;

    public IAlignmentTool Tool { get; }

    public string Id => Tool.Id;

    public string Name => Tool.Name;
}
