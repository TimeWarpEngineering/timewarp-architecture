#region Purpose
// Verifies FastEndpointSourceGenerator carries [EndpointAuthorize] onto the generated endpoint:
// policies, roles, authentication schemes, and the explicit anonymous opt-out.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_Authorization_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_Authorization_Tests>();

  private const string PolicyAuthorizedContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Policy = "AdminOnly")]
    public static partial class GetAdminStats
    {
        [ApiRoute("api/admin/stats", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Policies_For_EndpointAuthorize_Policy()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(PolicyAuthorizedContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Policies("AdminOnly")""");
    generatedCode.ShouldNotContain("AllowAnonymous");
    generatedCode.ShouldNotContain("RequireAuthorization");

    return Task.CompletedTask;
  }

  private const string RolesAuthorizedContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Roles = "Admin,Manager")]
    public static partial class GetTeamRoster
    {
        [ApiRoute("api/admin/roster", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Roles_For_EndpointAuthorize_Roles()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(RolesAuthorizedContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Roles("Admin", "Manager")""");
    generatedCode.ShouldNotContain("AllowAnonymous");

    return Task.CompletedTask;
  }

  private const string SchemesOnlyContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Agents;

    [ApiEndpoint]
    [EndpointAuthorize(AuthenticationSchemes = "Bearer")]
    public static partial class GetAgentIdentity
    {
        [ApiRoute("api/agents/me", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_AuthSchemes_Without_AllowAnonymous()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(SchemesOnlyContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""AuthSchemes("Bearer")""");
    generatedCode.ShouldNotContain("AllowAnonymous");
    generatedCode.ShouldNotContain("RequireAuthorization");

    return Task.CompletedTask;
  }

  // Task 158: Policy + multi-scheme AuthenticationSchemes together — the exact shape web-contracts
  // now use (e.g. GetRoles: Policy = CanViewRolesPage, AuthenticationSchemes =
  // "identity-session,mock-identity-session") so the generated FastEndpoint's AuthSchemes(...)
  // actually invokes non-default authentication handlers (mock-identity-session) that the named
  // policy alone did not reach.
  private const string PolicyAndSchemesContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Policy = "CanViewRolesPage", AuthenticationSchemes = "identity-session,mock-identity-session")]
    public static partial class GetRolesForTest
    {
        [ApiRoute("api/admin/roles", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Both_AuthSchemes_And_Policies_When_Both_Set()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(PolicyAndSchemesContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""AuthSchemes("identity-session", "mock-identity-session")""");
    generatedCode.ShouldContain("""Policies("CanViewRolesPage")""");
    generatedCode.ShouldNotContain("AllowAnonymous");
    generatedCode.ShouldNotContain("RequireAuthorization");

    return Task.CompletedTask;
  }

  // Task 110: explicit anonymous opt-out.
  private const string ExplicitAnonymousContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Hello;

    [ApiEndpoint]
    [EndpointAllowAnonymous("Public demo endpoint; no security surface.")]
    public static partial class SayHello
    {
        [ApiRoute("api/hello", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_AllowAnonymous_For_EndpointAllowAnonymous()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(ExplicitAnonymousContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("AllowAnonymous()");
    generatedCode.ShouldContain(
      "Intentionally anonymous. Public demo endpoint; no security surface.");
    generatedCode.ShouldNotContain("ProducesProblem(400");

    return Task.CompletedTask;
  }

  // Task 110: both markers present — [EndpointAuthorize] wins at generation (TWA0014 flags the
  // contradiction separately; the generator must still emit something deterministic).
  private const string BothMarkersContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Policy = "AdminOnly")]
    [EndpointAllowAnonymous("Contradictory marker for this test.")]
    public static partial class GetConflictedStats
    {
        [ApiRoute("api/admin/conflicted-stats", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Prefer_EndpointAuthorize_When_Both_Markers_Present()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(BothMarkersContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Policies("AdminOnly")""");
    generatedCode.ShouldNotContain("AllowAnonymous");

    return Task.CompletedTask;
  }

  public static Task Should_Leave_Endpoint_Emission_Unchanged_When_Operation_Is_Present()
  {
    const string source = """
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace TimeWarp.Contracts
      {
          [System.AttributeUsage(System.AttributeTargets.Class)]
          public sealed class OperationAttribute : System.Attribute
          {
              public OperationAttribute(string name) { }
              public string? Description { get; set; }
          }
      }

      namespace Test.Features.WeatherForecast
      {
          [ApiEndpoint]
          public static partial class GetWeatherForecasts
          {
              [TimeWarp.Contracts.Operation("weather.get", Description = "Forecast")]
              [ApiRoute("api/weatherForecasts", HttpVerb.Get)]
              public sealed partial class Query
              {
                  public int? Days { get; set; }
              }

              public sealed class Response { }
          }
      }
      """;

    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(source);
    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);
    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Get("api/weatherForecasts")""");
    generatedCode.ShouldNotContain("weather.get");
    generatedCode.ShouldNotContain("OperationName");
    return Task.CompletedTask;
  }
}
