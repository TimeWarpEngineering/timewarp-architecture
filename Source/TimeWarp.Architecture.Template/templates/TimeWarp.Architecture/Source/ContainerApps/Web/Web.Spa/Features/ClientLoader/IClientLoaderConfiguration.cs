namespace TimeWarp.Architecture.Features.ClientLoaders;

using System;

public interface IClientLoaderConfiguration
{
  TimeSpan DelayTimeSpan { get; }
}
