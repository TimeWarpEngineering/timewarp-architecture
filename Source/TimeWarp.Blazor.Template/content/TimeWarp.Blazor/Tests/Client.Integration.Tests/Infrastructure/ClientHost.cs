namespace TimeWarp.Blazor.Integration.Tests.Infrastructure.Client
{
  using Microsoft.Extensions.DependencyInjection;
  using System;

  [NotTest]
  public class ClientHost
  {

    public ClientHost(ServiceProvider aServiceProvider)
    {
      ServiceProvider = aServiceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
  }
}
