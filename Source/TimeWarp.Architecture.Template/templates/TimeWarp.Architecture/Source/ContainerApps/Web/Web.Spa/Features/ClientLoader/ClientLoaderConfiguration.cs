namespace TimeWarp.Architecture.Features.ClientLoaders;

public class ClientLoaderConfiguration : IClientLoaderConfiguration
{
  public TimeSpan DelayTimeSpan => TimeSpan.FromSeconds(10);
}
