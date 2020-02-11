namespace TimeWarp.Blazor.Features.ClientLoaders.Client
{
  using System;

  public interface IClientLoaderConfiguration
  {
    TimeSpan DelayTimeSpan { get; }
  }
}
