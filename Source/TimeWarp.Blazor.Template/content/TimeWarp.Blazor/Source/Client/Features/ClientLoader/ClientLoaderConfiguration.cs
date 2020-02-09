namespace TimeWarp.Blazor.ClientLoaderFeature
{
  using System;

  public class ClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromSeconds(10);
  }
}
