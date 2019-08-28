namespace BlazorHosted_CSharp.EndToEnd.Tests.Infrastructure
{
  using BlazorHosted_CSharp.Client.Features.ClientLoaderFeature;
  using System;

  public class TestClientLoaderConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
