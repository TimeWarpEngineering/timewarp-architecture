#region Purpose
// Delegate shape that lets each contract supply its own canned response, so mock API services can run the SPA without a backend.
#endregion

namespace TimeWarp.Foundation.Types;

/// <summary>
/// Builds a canned <typeparamref name="TResponse"/> for mock API services from the outbound request.
/// </summary>
public delegate TResponse MockResponseFactory<out TResponse>(IApiRequest request) where TResponse : class;
