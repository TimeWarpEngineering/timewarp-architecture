namespace TimeWarp.Blazor.Features.ClientLoaders
{
  using System;

  public interface IClientLoaderConfiguration
  {
    TimeSpan DelayTimeSpan { get; }
  }
}
