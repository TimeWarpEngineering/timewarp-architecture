namespace TimeWarp.Architecture.Configuration;

using System;
using System.Collections.Generic;
using static TimeWarp.Architecture.Configuration.ServiceCollectionOptions;

public class ServiceCollectionOptions : Dictionary<string, Service>
{
  public ServiceCollectionOptions() : base(StringComparer.OrdinalIgnoreCase) { }

  public class Service
  {
    public string Protocol { get; set; } = null!;
    public string Host { get; set; } = null!;
    public int Port { get; set; }
  }
}
