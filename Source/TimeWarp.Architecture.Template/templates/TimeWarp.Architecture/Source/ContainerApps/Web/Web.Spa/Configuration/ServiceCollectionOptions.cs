namespace TimeWarp.Architecture.Configuration;

using static ServiceCollectionOptions;

public class ServiceCollectionOptions() : Dictionary<string, Service>(StringComparer.OrdinalIgnoreCase)
{

  public class Service
  {
    public string Protocol { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int Port { get; set; }
  }
}
