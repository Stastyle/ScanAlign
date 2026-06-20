global using System.Numerics;
global using System.Text;

// Disambiguate our geometry primitive from System.Numerics.Plane (which we never use).
// Tracks in other assemblies that import both namespaces should add the same alias.
global using Plane = ScanAlign.Core.Model.Plane;
