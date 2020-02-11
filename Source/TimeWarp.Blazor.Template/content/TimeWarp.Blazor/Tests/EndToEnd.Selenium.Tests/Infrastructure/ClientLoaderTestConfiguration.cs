namespace TimeWarp.Blazor.EndToEnd.Tests.Infrastructure
{
  using System;
  using TimeWarp.Blazor.Features.ClientLoaders.Client;

  public class TestClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
