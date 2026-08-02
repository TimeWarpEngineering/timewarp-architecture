namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_RouteConflicts_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_RouteConflicts_Tests>();

  // Two [ApiEndpoint] contracts that map to the SAME route+verb — must raise TWE003 on ALL
  // parties and generate NONE of them (F-003).
  private const string ConflictingContracts = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.WeatherForecast;

    [ApiEndpoint]
    public static partial class GetWeatherForecasts
    {
        [ApiRoute("api/weather", HttpVerb.Get)]
        public sealed partial class Query { public int? Days { get; set; } }
        public sealed class Response { }
    }

    [ApiEndpoint]
    public static partial class GetCurrentWeather
    {
        [ApiRoute("api/weather", HttpVerb.Get)]
        public sealed partial class Query { public int? Days { get; set; } }
        public sealed class Response { }
    }
    """;

  public static Task Should_Detect_Route_Conflicts_On_All_Parties_And_Generate_None()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(ConflictingContracts);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    ImmutableArray<Diagnostic> conflicts = diagnostics.Where(d => d.Id == "TWE003").ToImmutableArray();
    conflicts.Length.ShouldBe(2, "TWE003 must fire once per party in the conflict group");

    foreach (Diagnostic conflict in conflicts)
    {
      string message = conflict.GetMessage(System.Globalization.CultureInfo.InvariantCulture);
      message.ShouldContain("api/weather");
      message.ShouldContain("GetWeatherForecasts");
      message.ShouldContain("GetCurrentWeather");
    }

    int generatedCount = runResult.Results.SelectMany(r => r.GeneratedSources).Count();
    generatedCount.ShouldBe(0, "no endpoint in a conflict group is generated");

    return Task.CompletedTask;
  }

  public static Task Should_Be_Stable_Across_Dual_Runs_Without_Phantom_Conflicts()
  {
    // F-003: static RouteRegistry self-conflicted on IDE incremental re-runs; Collect batch must not.
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Items;

      [ApiEndpoint]
      public static partial class GetItem
      {
          [ApiRoute("api/items/{id}", HttpVerb.Get)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    var compilation = CSharpCompilation.Create(
      "Test.Server",
      syntaxTrees: Array.Empty<SyntaxTree>(),
      references: new[]
      {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location),
        contract,
      },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var generator = new FastEndpointSourceGenerator();
    var options = new Dictionary<string, string>
    {
      ["build_property.EnableApiEndpointGeneration"] = "true",
    };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(generator.AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    driver = driver.RunGenerators(compilation);
    GeneratorDriverRunResult first = driver.GetRunResult();
    first.Results.SelectMany(r => r.Diagnostics).ShouldNotContain(d => d.Id == "TWE003");
    first.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(1);

    // Second run on the same driver (incremental path) — still one source, no phantom TWE003.
    driver = driver.RunGenerators(compilation);
    GeneratorDriverRunResult second = driver.GetRunResult();
    second.Results.SelectMany(r => r.Diagnostics).ShouldNotContain(d => d.Id == "TWE003");
    second.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(1);

    return Task.CompletedTask;
  }
}

public class FastEndpointSourceGenerator_OpenApi_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_OpenApi_Tests>();

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
        [ApiRoute("api/weatherForecasts", HttpVerb.Get)]
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

    // Default leaf feature tag plus additive [OpenApiTags] values (filter + OpenAPI WithTags).
    generatedCode.ShouldContain("Tags(");
    generatedCode.ShouldContain("WithTags(");
    generatedCode.ShouldContain("\"WeatherForecast\"");
    generatedCode.ShouldContain("\"Weather\"");
    generatedCode.ShouldContain("\"Forecasting\"");

    // Weather-only ExampleRequest was removed — summary/description only.
    generatedCode.ShouldNotContain("ExampleRequest");
    generatedCode.ShouldContain("s.Summary =");
    generatedCode.ShouldContain("s.Description =");

    return Task.CompletedTask;
  }
}

public class FastEndpointSourceGenerator_ShapeAndVerb_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_ShapeAndVerb_Tests>();

  public static Task Should_Report_TWE002_When_Missing_Query_Or_Command()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Broken;

      [ApiEndpoint]
      public static partial class NoRequestNested
      {
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    diagnostics.ShouldContain(d => d.Id == "TWE002" && d.Severity == DiagnosticSeverity.Error);
    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE007_For_Unknown_HttpVerb()
  {
    // Harness HttpVerb includes Trace (not in the allow-list) to exercise fail-closed conversion.
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Items;

      [ApiEndpoint]
      public static partial class TraceItem
      {
          [ApiRoute("api/items/trace", HttpVerb.Trace)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    diagnostics.ShouldContain(d => d.Id == "TWE007" && d.Severity == DiagnosticSeverity.Error);
    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);
    // Must not silently fall open to Get(...).
    string allSources = string.Join("\n", runResult.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
    allSources.ShouldNotContain("""Get("api/items/trace")""");

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE007_When_ApiRoute_Missing()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Broken;

      [ApiEndpoint]
      public static partial class NoApiRoute
      {
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    diagnostics.ShouldContain(d =>
      d.Id == "TWE007"
      && d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("missing ApiRoute"));
    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Report_TWE007_When_Route_Template_Empty()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Broken;

      [ApiEndpoint]
      public static partial class EmptyRoute
      {
          [ApiRoute("", HttpVerb.Get)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<Diagnostic> diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
    diagnostics.ShouldContain(d =>
      d.Id == "TWE007"
      && d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("empty route"));
    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Emit_Head_And_Options_Verbs()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Probe;

      [ApiEndpoint]
      public static partial class HeadProbe
      {
          [ApiRoute("api/probe", HttpVerb.Head)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }

      [ApiEndpoint]
      public static partial class OptionsProbe
      {
          [ApiRoute("api/probe", HttpVerb.Options)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(2);

    string all = string.Join("\n", generated.Select(g => g.SourceText.ToString()));
    all.ShouldContain("""Head("api/probe")""");
    all.ShouldContain("""Options("api/probe")""");
    runResult.Results.SelectMany(r => r.Diagnostics)
      .ShouldNotContain(d => d.Id == "TWE003" || d.Id == "TWE007");

    return Task.CompletedTask;
  }

  public static Task Should_Skip_ClientOnly_ApiEndpoint_Without_Emission()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly("""
      using TimeWarp.Architecture;
      using TimeWarp.Architecture.Attributes;

      namespace Test.Features.Mock;

      [ApiEndpoint]
      [ClientOnlyContract("SPA mock only — not hosted.")]
      public static partial class MockOnly
      {
          [ApiRoute("api/mock-only", HttpVerb.Get)]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }

      [ApiEndpoint]
      public static partial class NestedClientOnly
      {
          [ApiRoute("api/nested-mock", HttpVerb.Get)]
          [ClientOnlyContract("ClientOnly on nested Query.")]
          public sealed partial class Query { public string? Id { get; set; } }
          public sealed class Response { }
      }
      """);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    runResult.Results.SelectMany(r => r.GeneratedSources).Count().ShouldBe(0);
    // Generator skips silently; TWA0020 is the convention-analyzer surface.
    runResult.Results.SelectMany(r => r.Diagnostics)
      .ShouldNotContain(d => d.Id == "TWE002" || d.Id == "TWE003" || d.Id == "TWE007");

    return Task.CompletedTask;
  }
}
