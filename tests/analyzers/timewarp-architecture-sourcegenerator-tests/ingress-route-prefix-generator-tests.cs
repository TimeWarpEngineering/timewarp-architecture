namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

// Exercises IngressRoutePrefixGenerator: collapsing web-contracts [ApiRoute] templates to top-level
// api/<segment> prefixes, the [ClientOnlyContract]/non-api exclusions, deterministic ordering, the
// enabled/disabled gate, and the TWA0017/TWA0018 diagnostics. The generator scans REFERENCED
// assemblies, so each contract set is compiled into its own named metadata assembly.
public class IngressRoutePrefixGenerator_Tests
{
  // The 104-003 regression shape: five api/identity/* operations (collapse to one prefix), the
  // api/Roles admin set that the hand list had dropped (must now appear), a [ClientOnlyContract]
  // GetCurrentUser (must NOT appear), a non-api Analytics route (falls to the web catch-all), and a
  // multi-segment api/Users/Current/Profile (collapses to api/Users).
  private const string WebContracts = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Web.Features;

    [ApiEndpoint]
    public static partial class GetCurrentSession
    {
        [ApiRoute("api/identity/session", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class AddPasskey
    {
        [ApiRoute("api/identity/credentials/passkey", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class RevokeCredential
    {
        [ApiRoute("api/identity/credentials/{CredentialId:guid}/revoke", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class StartPasskeyRegistration
    {
        [ApiRoute("api/identity/passkey/register/options", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetAgentIdentity
    {
        [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetRoles
    {
        [ApiRoute("api/Roles", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetRole
    {
        [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetProfile
    {
        [ApiRoute("api/Users/Current/Profile", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class SayHello
    {
        [ApiRoute("api/Hello", HttpVerb.Get)]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    // Real client-only shape (matches get-current-user-contracts.cs): NO [ApiEndpoint] on the outer
    // type, [ClientOnlyContract] on the NESTED request. Excluded by the [ApiEndpoint] gate and the
    // nested-[ClientOnlyContract] check alike.
    public static partial class GetCurrentUser
    {
        [ApiRoute("api/GetCurrentUser", HttpVerb.Get)]
        [ClientOnlyContract("Client builds the identity locally; never a server endpoint.")]
        public sealed partial class Query { }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class TrackEvent
    {
        [ApiRoute("Analytics/TrackEvent", HttpVerb.Post)]
        public sealed partial class Command { }
        public sealed class Response { }
    }
    """;

  private static Dictionary<string, string> EnabledForWebContracts() => new()
  {
    ["EnableIngressRouteGeneration"] = "true",
    ["IngressWebContractAssemblies"] = "web-contracts",
    ["IngressReservedPathPrefixes"] = "grpc",
  };

  private static string GeneratedSource(GeneratorDriverRunResult runResult)
    => runResult.Results.SelectMany(r => r.GeneratedSources).Single().SourceText.ToString();

  public static Task Should_Collapse_Web_Contracts_To_Top_Level_Prefixes()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    string generated = GeneratedSource(runResult);

    // Exactly the four owned prefixes, in Ordinal order (uppercase before lowercase).
    generated.ShouldContain("public static class WebServerApiRoutePrefixes");
    int hello = generated.IndexOf("\"api/Hello\"", System.StringComparison.Ordinal);
    int roles = generated.IndexOf("\"api/Roles\"", System.StringComparison.Ordinal);
    int users = generated.IndexOf("\"api/Users\"", System.StringComparison.Ordinal);
    int identity = generated.IndexOf("\"api/identity\"", System.StringComparison.Ordinal);

    hello.ShouldBeGreaterThan(-1);
    roles.ShouldBeGreaterThan(hello);
    users.ShouldBeGreaterThan(roles);
    identity.ShouldBeGreaterThan(users);

    return Task.CompletedTask;
  }

  public static Task Should_Collapse_Multiple_Identity_Routes_To_One_Prefix()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    string generated = GeneratedSource(runResult);

    // Five api/identity/* operations collapse to exactly one prefix entry.
    int count = generated.Split(new[] { "\"api/identity\"" }, System.StringSplitOptions.None).Length - 1;
    count.ShouldBe(1);
    // The nested identity paths never appear as their own prefixes.
    generated.ShouldNotContain("\"api/identity/session\"");
    generated.ShouldNotContain("\"api/identity/credentials\"");

    return Task.CompletedTask;
  }

  public static Task Should_Exclude_ClientOnlyContract_Routes()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    // GetCurrentUser is [ClientOnlyContract] — it never reaches the server, so no ingress route.
    GeneratedSource(runResult).ShouldNotContain("GetCurrentUser");

    return Task.CompletedTask;
  }

  // Belt-and-braces: [ApiEndpoint] on the outer type but [ClientOnlyContract] on the NESTED request
  // must still be excluded (either placement means "not hosted"). An outer-only check would wrongly
  // include this.
  private const string ApiEndpointWithNestedClientOnly = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Web.Features;

    [ApiEndpoint]
    public static partial class LegacyThing
    {
        [ApiRoute("api/legacy", HttpVerb.Get)]
        [ClientOnlyContract("Hosted marker on the outer type, but client-only on the request — must be excluded.")]
        public sealed partial class Query { }
        public sealed class Response { }
    }
    """;

  public static Task Should_Exclude_ApiEndpoint_With_Nested_ClientOnlyContract()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(ApiEndpointWithNestedClientOnly, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    string generated = GeneratedSource(runResult);
    generated.ShouldNotContain("api/legacy");
    generated.ShouldContain("ImmutableArray<string>.Empty");

    return Task.CompletedTask;
  }

  public static Task Should_Skip_Non_Api_Routes()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    // Analytics/TrackEvent's first segment is not "api" — the Web.Server catch-all serves it, so it
    // is not an ingress carve-out.
    GeneratedSource(runResult).ShouldNotContain("Analytics");

    return Task.CompletedTask;
  }

  public static Task Should_Not_Generate_When_Disabled()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    var options = new Dictionary<string, string> { ["IngressWebContractAssemblies"] = "web-contracts" };
    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, options);

    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Emit_Empty_All_When_Enabled_With_No_Participating_Contracts()
  {
    // A contracts assembly with no [ApiEndpoint] api routes — enabled still emits, with an empty All,
    // so the AppHost foreach compiles in every flag combo.
    const string noRoutes = """
      namespace Test.Web.Empty;
      public static class Nothing { }
      """;
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(noRoutes, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    string generated = GeneratedSource(runResult);
    generated.ShouldContain("public static class WebServerApiRoutePrefixes");
    generated.ShouldContain("ImmutableArray<string>.Empty");

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWA0019_When_Configured_Assembly_Not_Found()
  {
    // web-contracts IS referenced, but IngressWebContractAssemblies names "web-contractz" (typo) —
    // matches nothing, so every carve-out would silently vanish. TWA0019 must fire, and All still
    // emits (empty).
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    var options = new Dictionary<string, string>
    {
      ["EnableIngressRouteGeneration"] = "true",
      ["IngressWebContractAssemblies"] = "web-contractz",
    };
    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, options);

    runResult.Diagnostics.ShouldContain(d => d.Id == "TWA0019");
    string generated = GeneratedSource(runResult);
    generated.ShouldContain("public static class WebServerApiRoutePrefixes");
    generated.ShouldContain("ImmutableArray<string>.Empty");

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWA0017_When_Web_Prefix_Shadows_Foreign_Contract()
  {
    // web-contracts hosts a route whose prefix (api/weatherforecast) equals an api-contracts route —
    // the web catch-all would capture requests meant for Api.Server.
    const string webCollides = """
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Web.Features;

      [ApiEndpoint]
      public static partial class GetWeather
      {
          [ApiRoute("api/weatherforecast", HttpVerb.Get)]
          public sealed partial class Query { }
          public sealed class Response { }
      }
      """;
    const string apiContracts = """
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Api.Features;

      [ApiEndpoint]
      public static partial class GetWeatherForecasts
      {
          [ApiRoute("api/weatherforecast", HttpVerb.Get)]
          public sealed partial class Query { }
          public sealed class Response { }
      }
      """;

    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(webCollides, "web-contracts");
    MetadataReference api = GeneratorTestHarness.CompileContractAssembly(apiContracts, "api-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web, api }, EnabledForWebContracts());

    runResult.Diagnostics.ShouldContain(d => d.Id == "TWA0017");

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWA0017_For_Reserved_Grpc_Prefix()
  {
    // A web route whose first segment collides with the reserved grpc prefix — the ingress would
    // capture it ahead of the grpc backend.
    const string webGrpc = """
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Web.Features;

      [ApiEndpoint]
      public static partial class Grpcish
      {
          [ApiRoute("grpc/reflection", HttpVerb.Post)]
          public sealed partial class Command { }
          public sealed class Response { }
      }
      """;
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(webGrpc, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    runResult.Diagnostics.ShouldContain(d => d.Id == "TWA0017");

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWA0018_For_NonDerivable_Prefixes()
  {
    // Bare "api" (no top-level segment) and a parameterized second segment cannot be collapsed to a
    // top-level ingress prefix.
    const string nonDerivable = """
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Web.Features;

      [ApiEndpoint]
      public static partial class BareApi
      {
          [ApiRoute("api", HttpVerb.Get)]
          public sealed partial class Query { }
          public sealed class Response { }
      }

      [ApiEndpoint]
      public static partial class ParamSecond
      {
          [ApiRoute("api/{tenant}/things", HttpVerb.Get)]
          public sealed partial class Query { }
          public sealed class Response { }
      }
      """;
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(nonDerivable, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    runResult.Diagnostics.Count(d => d.Id == "TWA0018").ShouldBe(2);
    // Neither produces a prefix entry.
    string generated = GeneratedSource(runResult);
    generated.ShouldContain("ImmutableArray<string>.Empty");

    return Task.CompletedTask;
  }
}
