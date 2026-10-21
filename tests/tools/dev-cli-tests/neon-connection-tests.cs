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

  public static Task Pooled_Should_AcceptServerAsTheHost()
  {
    NeonConnection.Problems(Parameter, Pooled().Replace("Host=", "Server=", StringComparison.Ordinal), pooled: true).ShouldBeEmpty();
    return Task.CompletedTask;
  }

  public static Task Pooled_Should_RejectChannelBindingDisabledAndAUri()
  {
    string disabled = Pooled().Replace("Channel Binding=Disable", "Channel Binding=Disabled", StringComparison.Ordinal);
    string problems = string.Join('\n', NeonConnection.Problems(Parameter, disabled, pooled: true));
    problems.ShouldContain("Channel Binding");
    problems.ShouldNotContain(Secret);

    const string uri = "postgresql://app:" + Secret + "@ep-example-pooler.neon.tech/app?sslmode=require";
    string uriProblems = string.Join('\n', NeonConnection.Problems(Parameter, uri, pooled: true));
    uriProblems.ShouldContain("not a URI");
    uriProblems.ShouldNotContain(Secret);
    uriProblems.ShouldNotContain("ep-example");
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

public class RegionDistance_Given_
{
  private const string Secret = "s3cret-value";
  private const string Endpoint = "ep-secret-pooler";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RegionDistance_Given_>();

  public static Task FarPair_Should_NameTheNearbyAzureRegionAndNotTheSecret()
  {
    string? warning = NeonConnection.RegionDistanceWarning(Pooled("ap-southeast-1"), "eastus");
    warning.ShouldNotBeNull();
    warning.ShouldContain("ap-southeast-1");
    warning.ShouldContain("southeastasia");
    warning.ShouldContain("eastus");
    warning.ShouldContain("Warning only");
    warning.ShouldNotContain(Secret);
    warning.ShouldNotContain(Endpoint);
    return Task.CompletedTask;
  }

  public static Task MatchingLocation_Should_BeSilent()
  {
    NeonConnection.RegionDistanceWarning(Pooled("ap-southeast-1"), "southeastasia").ShouldBeNull();
    NeonConnection.RegionDistanceWarning(Pooled("ap-southeast-1"), "Southeast Asia").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task HostWithoutAnAwsRegion_Should_BeSilent()
  {
    NeonConnection.RegionDistanceWarning(
      "Host=ep-example-pooler.neon.tech;Password=" + Secret, "eastus").ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task UnknownAwsRegion_Should_NameOnlyThatRegion()
  {
    string? warning = NeonConnection.RegionDistanceWarning(Pooled("xx-fake-1"), "eastus");
    warning.ShouldNotBeNull();
    warning.ShouldContain("xx-fake-1");
    warning.ShouldContain("No paired Azure region");
    warning.ShouldNotContain(Secret);
    warning.ShouldNotContain(Endpoint);
    return Task.CompletedTask;
  }

  public static Task UnsetLocation_Should_NameTheNearbyRegion()
  {
    string? warning = NeonConnection.RegionDistanceWarning(Pooled("ap-southeast-1"), null);
    warning.ShouldNotBeNull();
    warning.ShouldContain("not set");
    warning.ShouldContain("southeastasia");
    warning.ShouldNotContain(Secret);
    warning.ShouldNotContain(Endpoint);
    return Task.CompletedTask;
  }

  private static string Pooled(string awsRegion) =>
    "Host=" + Endpoint + "." + awsRegion + ".aws.neon.tech;Database=app;Username=app;Password=" + Secret
    + ";SSL Mode=Require;Timeout=30;Max Auto Prepare=0;No Reset On Close=true;Channel Binding=Disable";
}
