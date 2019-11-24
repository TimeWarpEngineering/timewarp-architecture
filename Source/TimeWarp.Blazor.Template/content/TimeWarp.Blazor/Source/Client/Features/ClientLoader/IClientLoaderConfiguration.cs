namespace TimeWarp.Blazor.Client.ClientLoaderFeature
{
  using System;

  public interface IClientLoaderConfiguration
  {
    TimeSpan DelayTimeSpan { get; }
  }
}