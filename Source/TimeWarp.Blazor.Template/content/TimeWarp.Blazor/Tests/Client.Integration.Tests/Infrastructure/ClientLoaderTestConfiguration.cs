namespace TimeWarp.Blazor.Client.Integration.Tests.Infrastructure
{
  using System;
  using TimeWarp.Blazor.Features.ClientLoaders;

  [NotTest]
  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
