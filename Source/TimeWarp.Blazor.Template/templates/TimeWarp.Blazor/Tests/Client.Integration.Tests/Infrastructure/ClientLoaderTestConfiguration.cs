namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using System;
  using TimeWarp.Blazor.Features.ClientLoaders;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
