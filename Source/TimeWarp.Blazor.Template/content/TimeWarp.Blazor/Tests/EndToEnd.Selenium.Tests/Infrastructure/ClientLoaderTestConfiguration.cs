namespace TimeWarp.Blazor.EndToEnd.Tests.Infrastructure
{
  using System;
  using TimeWarp.Blazor.Client.ClientLoaderFeature;

  public class TestClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
