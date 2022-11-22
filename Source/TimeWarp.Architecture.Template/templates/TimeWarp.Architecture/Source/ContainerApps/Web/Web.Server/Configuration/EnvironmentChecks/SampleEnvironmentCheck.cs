namespace TimeWarp.Architecture.Configuration;

public class SampleEnvironmentCheck
{
  private readonly ILogger Logger;
  public static string Description => "Sample Environment check";

  public SampleEnvironmentCheck(ILogger<SampleEnvironmentCheck> aLogger)
  {
    Logger = aLogger;
  }

  public void Check()
  {
    Logger.LogInformation($"Start {nameof(SampleEnvironmentCheck)} ");
    // Do something here.Throw exception to cause a failure.
    Logger.LogInformation($"Completed {nameof(SampleEnvironmentCheck)} ");
  }
}
