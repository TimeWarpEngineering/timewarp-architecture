namespace TimeWarp.Blazor.Configuration;

using System;
using System.Collections.Generic;
using static TimeWarp.Blazor.Configuration.ServiceCollection;

[SectionName("service")]
public class ServiceCollection : Dictionary<string, Service>
{
  public ServiceCollection() : base(StringComparer.OrdinalIgnoreCase) { }

  public class Service
  {
    public string Protocol { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
  }
}
