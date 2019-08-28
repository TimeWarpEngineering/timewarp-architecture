namespace TimeWarp.Blazor.Client.Features.ClientLoaderFeature
{
  using System;

  public class ClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromSeconds(10);
  }
}
