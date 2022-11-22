namespace TimeWarp.Architecture.Features.ClientLoaders;

public interface IClientLoaderConfiguration
{
  TimeSpan DelayTimeSpan { get; }
}
