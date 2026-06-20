namespace ScanAlign.Core.IO;

/// <summary>Shared PLY header model used by <see cref="PlyReader"/> and <see cref="PlyWriter"/>.</summary>
internal static class PlyFormat
{
    public enum DataFormat
    {
        Ascii,
        BinaryLittleEndian,
        BinaryBigEndian,
    }

    public sealed class Property
    {
        public required string Name { get; init; }
        public bool IsList { get; init; }
        public string CountType { get; init; } = string.Empty;
        public string ValueType { get; init; } = string.Empty;
    }

    public sealed class Element
    {
        public required string Name { get; init; }
        public required int Count { get; init; }
        public List<Property> Properties { get; } = new();
    }

    /// <summary>Byte size of a PLY scalar type (handles both the short and explicit-width names).</summary>
    public static int SizeOf(string type) => type switch
    {
        "char" or "uchar" or "int8" or "uint8" => 1,
        "short" or "ushort" or "int16" or "uint16" => 2,
        "int" or "uint" or "int32" or "uint32" or "float" or "float32" => 4,
        "double" or "float64" => 8,
        _ => throw new NotSupportedException($"Unsupported PLY type '{type}'."),
    };

    public static bool IsFloating(string type) =>
        type is "float" or "float32" or "double" or "float64";
}
