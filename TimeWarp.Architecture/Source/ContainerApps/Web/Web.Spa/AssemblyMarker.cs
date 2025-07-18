﻿namespace TimeWarp.Architecture.Web.Spa;

/// <summary>
/// Serves as a marker for the assembly, facilitating easy identification and reflection-based operations.
/// </summary>
/// <remarks>
/// This class is intended to be used as a reference point within the assembly for scenarios such as assembly scanning,
/// where a stable, known type is required to locate the assembly at runtime. The class is sealed to indicate it is not
/// designed for inheritance or extension, reinforcing its role as a simple marker.
/// 
/// Note: Blazor WebAssembly projects require sealed classes instead of interfaces for assembly markers due to 
/// IL trimming and resource collection compatibility issues. See ADR 0002-assembly-identification-with-assembly-marker
/// for detailed explanation of this exception to the standard interface pattern.
/// </remarks>
public sealed class AssemblyMarker;
