#region Purpose
// Exercises IngressRoutePrefixGenerator: collapsing web-contracts [ApiRoute] templates to top-level
// api/<segment> prefixes and the [ClientOnlyContract]/non-api exclusions. The generator scans
// REFERENCED assemblies, so each contract set is compiled into its own named metadata assembly.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using static TimeWarp.Architecture.SourceGenerator.Tests.IngressRoutePrefixGeneratorTestFixture;

public class IngressRoutePrefixGenerator_Exclusion_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IngressRoutePrefixGenerator_Exclusion_Tests>();

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

  // FQN match only: Other.Lib.ApiRouteAttribute shares the simple name but must not be discovered
  // by HostedRouteDiscovery.IsApiRouteAttribute (task 053-004). Fully qualify — harness has
  // global using TimeWarp.Foundation.Features.
  private const string ApiEndpointWithForeignApiRoute = """
    using TimeWarp.Architecture.Attributes;

    namespace Other.Lib
    {
        internal sealed class ApiRouteAttribute : System.Attribute
        {
            public ApiRouteAttribute(string route, int verb) { }
        }
    }

    namespace Test.Web.Features
    {
        [ApiEndpoint]
        public static partial class CollidedThing
        {
            [Other.Lib.ApiRoute("api/collided", 1)]
            public sealed partial class Query { }
            public sealed class Response { }
        }
    }
    """;

  public static Task Should_Ignore_Foreign_ApiRouteAttribute_Same_Simple_Name()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(ApiEndpointWithForeignApiRoute, "web-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, EnabledForWebContracts());

    string generated = GeneratedSource(runResult);
    generated.ShouldNotContain("api/collided");
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
}
