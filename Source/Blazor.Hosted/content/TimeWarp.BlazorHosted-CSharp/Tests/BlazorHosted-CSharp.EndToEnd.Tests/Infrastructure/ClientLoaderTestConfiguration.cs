namespace BlazorHosted_CSharp.EndToEnd.Tests.Infrastructure
{
  using BlazorHostedCSharp.Client.Features.ClientLoader;
  using System;

  public class ClientLoaderTestConfiguration : IClientLoaderConfiguration
  {
    public TimeSpan DelayTimeSpan => TimeSpan.FromMilliseconds(10);
  }
}
