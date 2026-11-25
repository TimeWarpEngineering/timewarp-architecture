#region Purpose
// One served web route that is not an OpenAPI operation, with the reason it is outside the document.
#endregion

#region Design
// foundation-server owns the shared exclusions (well-known discovery documents, liveness). A host
// passes its own routes (brand documents, hubs, sockets) built from the constants its Map calls
// use, because foundation cannot reference them. See is a path that documents the route; null
// omits it from x-timewarp-excluded-routes.
#endregion

namespace TimeWarp.Foundation;

/// <summary>
/// A served route listed in <c>x-timewarp-excluded-routes</c> and the document description.
/// </summary>
/// <param name="Method">HTTP method, upper case.</param>
/// <param name="Path">Path on the public origin.</param>
/// <param name="Reason">Why the route is not an operation, and what it returns.</param>
/// <param name="See">Optional path that documents the route.</param>
public sealed record OpenApiExcludedRoute(string Method, string Path, string Reason, string? See = null);
