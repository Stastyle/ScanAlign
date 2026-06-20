namespace ScanAlign.App.Services;

/// <summary>Abstracts file dialogs and error reporting so view-models stay testable.</summary>
public interface IDialogService
{
    /// <summary>Show an open-file dialog; returns the chosen path or null if cancelled.</summary>
    string? OpenMesh();

    /// <summary>Show a save-file dialog; returns the chosen path or null if cancelled.</summary>
    string? SaveMesh(string suggestedName);

    void Error(string message);
}
