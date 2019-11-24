namespace TimeWarp.Blazor.EndToEnd.Tests.Infrastructure
{
  using TimeWarp.Blazor.Client.ClientLoaderFeature;
  using System;

  public class TestClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
