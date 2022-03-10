namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Request used for all Requests
/// </summary>
/// <remarks>
/// Requests should be mutable reference types. 
/// </remarks>
public abstract record BaseRequest : BaseMessage { }
