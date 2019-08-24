namespace BlazorHostedCSharp.Client.Features.ClientLoader
{
  using System;

  public interface IClientLoaderConfiguration
  {
    TimeSpan DelayTimeSpan { get; }
  }
}
