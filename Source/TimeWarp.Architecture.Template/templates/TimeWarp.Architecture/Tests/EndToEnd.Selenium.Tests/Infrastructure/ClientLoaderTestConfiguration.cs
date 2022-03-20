namespace TimeWarp.Architecture.EndToEnd.Tests.Infrastructure
{
  using System;
  using TimeWarp.Architecture.Features.ClientLoaders;

  public class TestClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
