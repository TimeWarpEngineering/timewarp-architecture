#region Purpose
// FastEndpoint assembly-marker + ApiEndpointContractAssemblies allow-list (task 006-001).
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Globalization;
using System.IO;

public class FastEndpointSourceGenerator_ContractAssemblies_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_ContractAssemblies_Tests>();

  private const string WeatherContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.WeatherForecast;

    [ApiEndpoint]
    public static partial class GetWeatherForecasts
    {
        [ApiRoute("api/weatherForecasts", HttpVerb.Get)]
        public sealed partial class Query
        {
            public int? Days { get; set; }
        }

        public sealed class Response { }
    }
    """;

  private const string RolesContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin.Roles;

    [ApiEndpoint]
    public static partial class CreateRole
    {
        [ApiRoute("api/admin/roles", HttpVerb.Post)]
        public sealed partial class Command
        {
            public string? Name { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Stamp_Assembly_Marker_When_ApiEndpoint_Exists()
  {
    string generated = ConcatGenerated(RunOnSource(WeatherContract));
    generated.ShouldContain("internal sealed class ApiEndpointsEmbeddedAttribute : System.Attribute");
    generated.ShouldContain("[assembly: TimeWarp.Architecture.ApiEndpointsEmbedded]");
    generated.ShouldNotContain("GetWeatherForecastsEndpoint");
    return Task.CompletedTask;
  }

  public static Task Should_Not_Stamp_Assembly_Marker_Without_ApiEndpoint()
  {
    string generated = ConcatGenerated(RunOnSource("namespace Sample; public class Nothing;"));
    generated.ShouldNotContain("[assembly: TimeWarp.Architecture.ApiEndpointsEmbedded]");
    generated.ShouldNotContain("ApiEndpointsEmbeddedAttribute");
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE008_When_Allow_List_Empty()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run([contract], enabled: true, contractAssemblies: null);

    runResult.Diagnostics.ShouldContain(static d => d.Id == "TWE008" && d.Severity == DiagnosticSeverity.Error);
    runResult.Results.Sum(static r => r.GeneratedSources.Length).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE008_When_Configured_Assembly_Not_Found()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(
      [contract],
      enabled: true,
      contractAssemblies: "web-contractz");

    runResult.Diagnostics.ShouldContain(static d =>
      d.Id == "TWE008"
      && d.GetMessage(CultureInfo.InvariantCulture).Contains("web-contractz"));
    runResult.Results.Sum(static r => r.GeneratedSources.Length).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE008_When_Configured_Assembly_Is_Unmarked()
  {
    MetadataReference unmarked = GeneratorTestHarness.CompileContractAssembly(
      WeatherContract,
      "Test.Contracts",
      stampMarker: false);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(
      [unmarked],
      enabled: true,
      contractAssemblies: "Test.Contracts");

    runResult.Diagnostics.ShouldContain(static d =>
      d.Id == "TWE008"
      && d.GetMessage(CultureInfo.InvariantCulture).Contains("ApiEndpointsEmbedded"));
    runResult.Results.Sum(static r => r.GeneratedSources.Length).ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Only_From_Allow_Listed_Marked_Assembly()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(RolesContract, "web-contracts");
    MetadataReference api = GeneratorTestHarness.CompileContractAssembly(WeatherContract, "api-contracts");

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(
      [web, api],
      enabled: true,
      contractAssemblies: "web-contracts");

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(static r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(1);
    string generatedCode = generated[0].SourceText.ToString();
    generatedCode.ShouldContain("CreateRoleEndpoint");
    generatedCode.ShouldNotContain("GetWeatherForecastsEndpoint");
    runResult.Diagnostics.ShouldNotContain(static d => d.Id == "TWE008");
    return Task.CompletedTask;
  }

  public static Task Should_Skip_Unmarked_Foreign_Assembly_Even_When_It_Has_ApiEndpoint()
  {
    MetadataReference web = GeneratorTestHarness.CompileContractAssembly(RolesContract, "web-contracts");
    MetadataReference unmarkedApi = GeneratorTestHarness.CompileContractAssembly(
      WeatherContract,
      "api-contracts",
      stampMarker: false);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(
      [web, unmarkedApi],
      enabled: true,
      contractAssemblies: "web-contracts");

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(static r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(1);
    generated[0].SourceText.ToString().ShouldContain("CreateRoleEndpoint");
    generated[0].SourceText.ToString().ShouldNotContain("GetWeatherForecastsEndpoint");
    return Task.CompletedTask;
  }

  private static GeneratorDriverRunResult RunOnSource(string source)
  {
    CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse);
    CSharpCompilation compilation = CSharpCompilation.Create(
      "web-contracts",
      [
        CSharpSyntaxTree.ParseText(source, parseOptions),
        CSharpSyntaxTree.ParseText(GeneratorTestHarness.SupportStubs, parseOptions),
      ],
      ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(static path => path.Length > 0)
        .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location)),
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      ImmutableArray.Create(new FastEndpointSourceGenerator().AsSourceGenerator()));
    return driver.RunGenerators(compilation).GetRunResult();
  }

  private static string ConcatGenerated(GeneratorDriverRunResult runResult)
    => string.Join("\n", runResult.Results.SelectMany(static r => r.GeneratedSources).Select(static s => s.SourceText.ToString()));
}
