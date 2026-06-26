namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_RouteConflicts_Tests
{
  // Two [ApiEndpoint] contracts that map to the SAME route+verb — must raise the TWE003 conflict.
  private const string ConflictingContracts = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.WeatherForecast;

    [ApiEndpoint]
    public static partial class GetWeatherForecasts
    {
        [RouteMixin("api/weather", HttpVerb.Get)]
        public sealed partial class Query { public int? Days { get; set; } }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetCurrentWeather
    {
        [RouteMixin("api/weather", HttpVerb.Get)]
        public sealed partial class Query { public int? Days { get; set; } }
        public sealed class Response { }
    }
    """;

  public static Task Should_Detect_Route_Conflicts()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(ConflictingContracts);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    bool hasRouteConflict = diagnostics.Any(d =>
      d.Id == "TWE003" &&
      d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("api/weather", System.StringComparison.Ordinal));
    hasRouteConflict.ShouldBeTrue();

    return Task.CompletedTask;
  }
}

public class FastEndpointSourceGenerator_OpenApi_Tests
{
  private const string DocumentedContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.WeatherForecast;

    [ApiEndpoint]
    [OpenApiTags("Weather", "Forecasting")]
    public static partial class GetWeatherForecasts
    {
        /// <summary>
        /// Gets weather forecasts for specified days
        /// </summary>
        /// <remarks>
        /// Retrieves detailed weather forecasts including temperature and conditions
        /// </remarks>
        [RouteMixin("api/weatherForecasts", HttpVerb.Get)]
        public sealed partial class Query
        {
            /// <summary>
            /// Number of days to forecast
            /// </summary>
            public int? Days { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Generate_OpenApi_Documentation()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(DocumentedContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(1);

    string generatedCode = generated[0].SourceText.ToString();

    // Summary/remarks flow through from the contract's XML docs (cross-assembly).
    generatedCode.ShouldContain("Gets weather forecasts for specified days");
    generatedCode.ShouldContain("Retrieves detailed weather forecasts including temperature and conditions");

    // OpenApiTags values appear in the generated Tags(...) call.
    generatedCode.ShouldContain("Tags(");
    generatedCode.ShouldContain("\"Weather\"");
    generatedCode.ShouldContain("\"Forecasting\"");

    return Task.CompletedTask;
  }
}
