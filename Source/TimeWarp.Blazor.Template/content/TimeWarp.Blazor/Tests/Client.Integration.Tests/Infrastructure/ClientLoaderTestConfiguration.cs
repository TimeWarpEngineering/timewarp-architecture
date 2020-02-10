namespace TimeWarp.Blazor.Features.ClientLoaders
{
  using System;

  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
