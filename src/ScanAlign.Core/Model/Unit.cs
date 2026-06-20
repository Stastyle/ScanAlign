namespace ScanAlign.Core.Model;

/// <summary>
/// Length unit a mesh is expressed in. <see cref="Unknown"/> means undetermined —
/// the app never silently rescales an unknown unit; it asks the user.
/// </summary>
public enum Unit
{
    Unknown,
    Millimeter,
    Centimeter,
    Meter,
    Inch,
}
