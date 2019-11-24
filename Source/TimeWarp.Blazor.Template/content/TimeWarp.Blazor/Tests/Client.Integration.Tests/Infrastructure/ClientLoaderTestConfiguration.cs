namespace TimeWarp.Blazor.Client.ClientLoaderFeature
{
  using System;

  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}