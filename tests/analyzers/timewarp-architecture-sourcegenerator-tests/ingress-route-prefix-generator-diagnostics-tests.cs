#region Purpose
// Exercises IngressRoutePrefixGenerator: the enabled/disabled gate and the TWA0017/TWA0018/TWA0019
// diagnostics. Each contract set is compiled into its own named metadata assembly.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;
using static TimeWarp.Architecture.SourceGenerator.Tests.IngressRoutePrefixGeneratorTestFixture;

public class IngressRoutePrefixGenerator_Diagnostics_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IngressRoutePrefixGenerator_Diagnostics_Tests>();

  public static Task Should_Not_Generate_When_Disabled()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(WebContracts, "web-contracts");

    var options = new Dictionary<string, string> { ["IngressWebContractAssemblies"] = "web-contracts" };
    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { web }, options);

    runResult.Results.Sum(r => r.GeneratedSources.Length).ShouldBe(0);

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

    Dictionary<string, string> options = new()
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

  public static Task Should_Report_TWA0019_When_Configured_Assembly_Is_Unmarked()
  {
    // web-contracts is referenced and listed, but lacks [assembly: ApiEndpointsEmbedded] — discovery
    // skips it, so All would silently empty without TWA0019. Marker absence must fire the same
    // diagnostic as a typo.
    MetadataReference unmarked = GeneratorTestHarness.CompileContractAssembly(
      WebContracts,
      "web-contracts",
      stampMarker: false);

    Dictionary<string, string> options = new()
    {
      ["EnableIngressRouteGeneration"] = "true",
      ["IngressWebContractAssemblies"] = "web-contracts",
    };
    GeneratorDriverRunResult runResult = GeneratorTestHarness.RunIngress(new[] { unmarked }, options);

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
