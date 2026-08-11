#region Purpose
// Common ancestor for all request contracts, marking the request side of the message hierarchy.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// Base Request used for all Requests
/// </summary>
/// <remarks>
/// Requests should be mutable reference types.
/// </remarks>
public abstract class BaseRequest : BaseMessage;
