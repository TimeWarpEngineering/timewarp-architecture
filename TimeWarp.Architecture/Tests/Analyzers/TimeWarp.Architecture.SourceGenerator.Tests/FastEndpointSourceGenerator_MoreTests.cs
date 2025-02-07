namespace TimeWarp.Architecture.SourceGenerator.Tests;

public class FastEndpointSourceGenerator_RouteConflicts_Tests
{
    public static Task Should_Detect_Route_Conflicts()
    {
        const string TestCode1 = @"
using TimeWarp.Architecture.SourceGenerator;
using OneOf;

namespace Test.Features.WeatherForecast;

[ApiEndpoint]
public static partial class GetWeatherForecasts
{
    [RouteMixin(""api/weather"", HttpVerb.Get)]
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        public int? Days { get; set; }
    }

    public sealed class Response { }
}";

        const string TestCode2 = @"
using TimeWarp.Architecture.SourceGenerator;
using OneOf;

namespace Test.Features.WeatherForecast;

[ApiEndpoint]
public static partial class GetCurrentWeather
{
    [RouteMixin(""api/weather"", HttpVerb.Get)]
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        public int? Days { get; set; }
    }

    public sealed class Response { }
}";

        (string, string)[] sources = new[]
        {
            (typeof(ApiEndpointAttribute).Assembly.GetName().Name + "1.cs", TestCode1),
            (typeof(ApiEndpointAttribute).Assembly.GetName().Name + "2.cs", TestCode2)
        };

        MetadataReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location)
        };

        // Create compilation
        var compilation = CSharpCompilation.Create(
            "test",
            sources.Select(s => CSharpSyntaxTree.ParseText(s.Item2)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create an instance of the source generator
        FastEndpointSourceGenerator generator = new();

        // Run the generator and get results
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();
        
        // Verify that a diagnostic was reported
        var diagnostics = runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray();
        bool hasRouteConflict = diagnostics.Any(d => d.Id == "TWE003" && d.GetMessage() != null && d.GetMessage().Contains("api/weather"));
        hasRouteConflict.Should().BeTrue();

        return Task.CompletedTask;
    }
}

public class FastEndpointSourceGenerator_OpenApi_Tests
{
    public static Task Should_Generate_OpenApi_Documentation()
    {
        const string TestCode = @"
using TimeWarp.Architecture.SourceGenerator;
using OneOf;

namespace Test.Features.WeatherForecast;

[ApiEndpoint]
[OpenApiTags(""Weather"", ""Forecasting"")]
public static partial class GetWeatherForecasts
{
    /// <summary>
    /// Gets weather forecasts for specified days
    /// </summary>
    /// <remarks>
    /// Retrieves detailed weather forecasts including temperature and conditions
    /// </remarks>
    [RouteMixin(""api/weatherForecasts"", HttpVerb.Get)]
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        /// <summary>
        /// Number of days to forecast
        /// </summary>
        /// <example>5</example>
        public int? Days { get; set; }
    }

    public sealed class Response { }
}";

        (string, string)[] sources = new[] { (typeof(ApiEndpointAttribute).Assembly.GetName().Name + ".cs", TestCode) };
        MetadataReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ApiEndpointAttribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "test",
            sources.Select(s => CSharpSyntaxTree.ParseText(s.Item2)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        FastEndpointSourceGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        // Get the generated code
        var generatedSyntaxTrees = runResult.Results.SelectMany(r => r.GeneratedSources).Select(g => g.SyntaxTree).ToImmutableArray();
        generatedSyntaxTrees.Length.Should().Be(1);
        string generatedCode = generatedSyntaxTrees[0].ToString();

        // Verify OpenAPI documentation is included
        generatedCode.Should().Contain("Gets weather forecasts for specified days");
        generatedCode.Should().Contain("Retrieves detailed weather forecasts including temperature and conditions");
        generatedCode.Should().Contain(@"Tags(""Weather"", ""Forecasting"")");

        return Task.CompletedTask;
    }
}