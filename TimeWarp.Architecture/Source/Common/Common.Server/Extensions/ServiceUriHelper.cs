namespace TimeWarp.Architecture.Extensions;

public static class ServiceUriHelper
{
  public static Uri? GetServiceHttpUri(string resourceName, int index = 0) =>
    GetServiceUri(resourceName,"http", index);

  public static Uri? GetServiceHttpsUri(string resourceName, int index = 0) =>
    GetServiceUri(resourceName, "https", index);

  private static Uri? GetServiceUri(string resourceName, string endpointName, int index)
  {
    Guard.Against.NullOrWhiteSpace(resourceName, nameof(resourceName));
    Guard.Against.NullOrWhiteSpace(endpointName, nameof(endpointName));
    Guard.Against.Negative(index, nameof(index));

    string? url = Environment.GetEnvironmentVariable($"services__{resourceName}__{endpointName}__{index}");

    return url is null ? null : new Uri(url);
  }
}
