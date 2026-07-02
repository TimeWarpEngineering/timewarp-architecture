#region Purpose
// Template placeholder demonstrating how to author an Oakton environment check.
#endregion

#region Design
// Exists so projects generated from this template have a working example of the
// CheckEnvironment registration pattern (see Program.ConfigureEnvironmentChecks) to copy for
// real preconditions; a check signals failure by throwing.
// LoggerMessage.Define is used to model the repo's allocation-free logging convention.
#endregion

#nullable enable
namespace TimeWarp.Architecture.Configuration;

public class SampleEnvironmentCheck
{
  private static readonly Action<ILogger, Exception?> LogStart =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(1, nameof(LogStart)),
      $"Start {nameof(SampleEnvironmentCheck)} "
    );

  private static readonly Action<ILogger, Exception?> LogCompleted =
    LoggerMessage.Define
    (
      LogLevel.Information,
      new EventId(2, nameof(LogCompleted)),
      $"Completed {nameof(SampleEnvironmentCheck)} "
    );

  private readonly ILogger Logger;
  public static string Description => "Sample Environment check";

  public SampleEnvironmentCheck(ILogger<SampleEnvironmentCheck> logger)
  {
    Logger = logger;
  }

  public void Check()
  {
    LogStart(Logger, null);
    // Do something here.Throw exception to cause a failure.
    LogCompleted(Logger, null);
  }
}
