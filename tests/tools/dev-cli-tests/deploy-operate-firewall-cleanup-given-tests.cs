#region Purpose
// Gates the firewall-rule cleanup on failure.
#endregion

// ReSharper disable InconsistentNaming
namespace DeployOperate_;

public class FirewallCleanup_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FirewallCleanup_Given_>();

  private static readonly ProcessStep Create = new("az", ["create"]);
  private static readonly ProcessStep Bundle = new("bundle", ["--connection", "x"], Capture: true);
  private static readonly ProcessStep Delete = new("az", ["delete"]);
  private static readonly Action<string> NoReport = _ => { };

  private static Func<ProcessStep, CancellationToken, Task<int>> Runner(List<string> log, Func<ProcessStep, int> exitCode) =>
    (step, _) =>
    {
      log.Add(step.Arguments[0]);
      return Task.FromResult(exitCode(step));
    };

  public static async Task Success_Should_CreateRunAndDelete()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployAca.RunAcaBundleAsync(Create, Bundle, Delete, Runner(log, _ => 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "--connection", "delete"]);
    result.ShouldBe(new AcaMigrationResult(0, 0, null));
  }

  public static async Task FailedBundle_Should_StillDeleteTheRuleAndReportTheBundlesExitCode()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployAca.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Bundle ? 3 : 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "--connection", "delete"]);
    result.ExitCode.ShouldBe(3);
    result.CleanupFailure.ShouldBeNull();
  }

  public static async Task FailedCreate_Should_SkipTheBundleAndStillDelete()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployAca.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Create ? 2 : 0), NoReport, CancellationToken.None);
    log.ShouldBe(["create", "delete"]);
    result.ExitCode.ShouldBe(2);
  }

  public static async Task ThrowingBundle_Should_StillDeleteTheRule()
  {
    List<string> log = [];
    Task<int> Run(ProcessStep step, CancellationToken _)
    {
      log.Add(step.Arguments[0]);
      return step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(0);
    }

    await Should.ThrowAsync<OperationCanceledException>(() => DeployAca.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, CancellationToken.None));
    log.ShouldBe(["create", "--connection", "delete"]);
  }

  public static async Task CancelledToken_Should_NotCancelTheDelete()
  {
    using CancellationTokenSource cancelled = new();
    await cancelled.CancelAsync();
    CancellationToken deleteToken = default;
    Task<int> Run(ProcessStep step, CancellationToken token)
    {
      if (step == Delete)
      {
        deleteToken = token;
      }

      return Task.FromResult(step == Bundle ? 130 : 0);
    }

    await DeployAca.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, cancelled.Token);
    deleteToken.IsCancellationRequested.ShouldBeFalse();
  }

  public static async Task FailedDelete_Should_FailTheRunAndPrintTheCommand()
  {
    List<string> log = [];
    AcaMigrationResult result = await DeployAca.RunAcaBundleAsync(
      Create, Bundle, Delete, Runner(log, step => step == Delete ? 1 : 0), NoReport, CancellationToken.None);
    result.ExitCode.ShouldBe(1);
    result.CleanupExitCode.ShouldBe(1);
    result.CleanupFailure.ShouldBe("exit 1");

    string message = DeployAca.FirewallCleanupFailedMessage("rg", "pg1", "sub", "exit 1");
    message.ShouldContain("could NOT be deleted");
    message.ShouldContain("az postgres flexible-server firewall-rule delete --resource-group rg --name pg1 --subscription sub --rule-name operator-migrate --yes");
  }

  public static async Task ThrowingBundleAndFailedDelete_Should_StillReportTheCleanupFailure()
  {
    List<string> reported = [];
    static Task<int> Run(ProcessStep step, CancellationToken _)
    {
      return step == Bundle ? throw new OperationCanceledException("Ctrl+C") : Task.FromResult(step == Delete ? 1 : 0);
    }

    await Should.ThrowAsync<OperationCanceledException>(
      () => DeployAca.RunAcaBundleAsync(Create, Bundle, Delete, Run, reported.Add, CancellationToken.None));
    reported.ShouldBe(["exit 1"]);
  }

  public static async Task ThrowingDelete_Should_BeReportedNotSwallowed()
  {
    static Task<int> Run(ProcessStep step, CancellationToken _)
    {
      return step == Delete ? throw new InvalidOperationException("az vanished") : Task.FromResult(0);
    }

    AcaMigrationResult result = await DeployAca.RunAcaBundleAsync(Create, Bundle, Delete, Run, NoReport, CancellationToken.None);
    result.ExitCode.ShouldBe(1);
    result.CleanupFailure.ShouldBe("az vanished");
  }
}
