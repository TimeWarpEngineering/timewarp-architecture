namespace TimeWarp.Blazor.Integration.Tests.Infrastructure.Client
{
  using System;
  using TimeWarp.Blazor.Features.ClientLoaders.Client;

  [NotTest]
  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
