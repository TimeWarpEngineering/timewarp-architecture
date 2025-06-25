namespace TimeWarp.Architecture.Api.Domain;

/// <summary>
/// Serves as a marker for the assembly, facilitating easy identification and reflection-based operations.
/// </summary>
/// <remarks>
/// This interface is intended to be used as a reference point within the assembly for scenarios such as assembly scanning,
/// where a stable, known type is required to locate the assembly at runtime. The interface prevents instantiation,
/// reinforcing its role as a simple marker.
/// </remarks>

public interface IAssemblyMarker;
