#region Purpose
// Gates the aca-neon connection-string shape check: pooled versus direct, without the secret in the refusal.
#endregion

namespace NeonConnection_;

public class Problems_Given_
{
  private const string Parameter = "neon-pooled-connection-string";
  private const string Secret = "s3cret-value";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Problems_Given_>();

  public static Task Pooled_Should_AcceptThePoolerShape()
  {
    NeonConnection.Problems(Parameter, Pooled(), pooled: true).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Direct_Should_AcceptANonPoolerHost()
  {
    NeonConnection.Problems("neon-direct-connection-string", Direct(), pooled: false).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Pooled_Should_RejectADirectHostAndThePoolerKnobs()
  {
    string problems = string.Join('\n', NeonConnection.Problems(Parameter, Direct(), pooled: true));

    problems.ShouldContain("-pooler");
    problems.ShouldContain("Max Auto Prepare");
    problems.ShouldContain("No Reset On Close");
    problems.ShouldContain("Channel Binding");
    problems.ShouldNotContain(Secret);
    problems.ShouldNotContain("ep-example");
    return Task.CompletedTask;
  }

  public static Task Direct_Should_RejectThePoolerHost()
  {
    string problems = string.Join('\n', NeonConnection.Problems("neon-direct-connection-string", Pooled(), pooled: false));

    problems.ShouldContain("must not use the pooler host");
    problems.ShouldNotContain(Secret);
    return Task.CompletedTask;
  }

  public static Task Either_Should_RequireSslAndAWakeUpTimeout()
  {
    string problems = string.Join('\n', NeonConnection.Problems(Parameter, "Host=ep-example-pooler.neon.tech;Password=" + Secret, pooled: true));

    problems.ShouldContain("SSL Mode");
    problems.ShouldContain("Timeout");
    problems.ShouldNotContain(Secret);
    return Task.CompletedTask;
  }

  public static Task Pooled_Should_RejectAKeepalive()
  {
    string connectionString = Pooled() + ";Keepalive=30";
    string problems = string.Join('\n', NeonConnection.Problems(Parameter, connectionString, pooled: true));

    problems.ShouldContain("Keepalive");
    problems.ShouldNotContain(Secret);
    return Task.CompletedTask;
  }

  public static Task Empty_Should_NotEchoAnythingElse()
  {
    NeonConnection.Problems(Parameter, "  ", pooled: true).ShouldBe([$"Neon connection string '{Parameter}' is empty."]);
    return Task.CompletedTask;
  }

  private static string Pooled() =>
    "Host=ep-example-pooler.neon.tech;Database=app;Username=app;Password=" + Secret
    + ";SSL Mode=Require;Timeout=30;Max Auto Prepare=0;No Reset On Close=true;Channel Binding=Disable";

  private static string Direct() =>
    "Host=ep-example.neon.tech;Database=app;Username=app;Password=" + Secret + ";SSL Mode=VerifyFull;Connection Timeout=30";
}
