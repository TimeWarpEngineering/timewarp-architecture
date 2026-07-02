#region Purpose
// Resolves sibling-service base URIs from Aspire service-discovery environment variables.
#endregion

#region Design
// Reads the services__{resource}__{endpoint}__{index} variables the Aspire AppHost injects, so
// resourceName must exactly match the AppHost resource name (see ServiceNames) — a mismatch
// resolves to null, which typically surfaces later as a null BaseAddress on server-side clients.
// Returns null instead of throwing so callers can distinguish "not orchestrated by Aspire"
// and fall back to configured URLs.
#endregion

namespace TimeWarp.Foundation.Extensions;

public static class ServiceUriHelper
{
  public static Uri? GetServiceHttpUri(string resourceName, int index = 0) =>
    GetServiceUri(resourceName,endpointName: "http", index);

  public static Uri? GetServiceHttpsUri(string resourceName, int index = 0) =>
    GetServiceUri(resourceName, endpointName: "https", index);

  private static Uri? GetServiceUri(string resourceName, string endpointName, int index)
  {
    Guard.Against.NullOrWhiteSpace(resourceName, nameof(resourceName));
    Guard.Against.NullOrWhiteSpace(endpointName, nameof(endpointName));
    Guard.Against.Negative(index, nameof(index));

    string? url = Environment.GetEnvironmentVariable($"services__{resourceName}__{endpointName}__{index}");

    return url is null ? null : new Uri(url);
  }
}
