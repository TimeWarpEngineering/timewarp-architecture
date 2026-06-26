namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_Tests
{
  // A minimal [ApiEndpoint] contract. The generator finds it by scanning REFERENCED assemblies
  // (cross-assembly, per task 007), so the harness compiles this into a separate assembly.
  private const string WeatherContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.WeatherForecast;

    [ApiEndpoint]
    public static partial class GetWeatherForecasts
    {
        [RouteMixin("api/weatherForecasts", HttpVerb.Get)]
        public sealed partial class Query
        {
            public int? Days { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Generate_Endpoint_From_Contract()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(1);

    string generatedCode = generated[0].SourceText.ToString();
    generatedCode.ShouldContain("namespace Test.Features.WeatherForecast;");
    generatedCode.ShouldContain("public class GetWeatherForecastsEndpoint");
    generatedCode.ShouldContain("BaseFastEndpoint<GetWeatherForecasts.Query, GetWeatherForecasts.Response>");
    generatedCode.ShouldContain("""Get("api/weatherForecasts")""");
    generatedCode.ShouldContain("AllowAnonymous()");

    return Task.CompletedTask;
  }

  public static Task Should_Not_Generate_When_Disabled()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    // EnableApiEndpointGeneration is not set (defaults to false).
    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: false);

    int generatedCount = runResult.Results.SelectMany(r => r.GeneratedSources).Count();
    generatedCount.ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Generate_When_Explicitly_Enabled()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    int generatedCount = runResult.Results.SelectMany(r => r.GeneratedSources).Count();
    generatedCount.ShouldBe(1);

    return Task.CompletedTask;
  }
}
