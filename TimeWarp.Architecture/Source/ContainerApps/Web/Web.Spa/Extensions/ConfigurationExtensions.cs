namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
  public static Uri? GetServiceHttpUri(this IConfiguration config, string name) =>
    config.GetServiceUri(name, 0);

  public static Uri? GetServiceHttpsUri(this IConfiguration config, string name) =>
    config.GetServiceUri(name, 1);

  private static Uri? GetServiceUri(this IConfiguration config, string name, int index)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    string? url = config[$"services:{name}:{index}"];

    return url is null ? null : new Uri(url);
  }
}
