using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace TimeWarp.Architecture.SourceGenerator.Tests;

public class FastEndpointSourceGenerator_Tests
{
    public static async Task Should_Generate_Endpoint_From_Contract()
    {
        const string TestCode = @"
using TimeWarp.Architecture.SourceGenerator;
using OneOf;

namespace Test.Features.WeatherForecast;

[ApiEndpoint]
public static partial class GetWeatherForecasts
{
    [RouteMixin(""api/weatherForecasts"", HttpVerb.Get)]
    public sealed partial class Query : IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
    {
        public int? Days { get; set; }
    }

    public sealed class Response
    {
        public IEnumerable<WeatherForecast> WeatherForecasts { get; set; } = default!;
    }
}";

        var expectedGeneratedCode = @"
using FastEndpoints;
using OneOf;

namespace Test.Features.WeatherForecast;

public class GetWeatherForecastsEndpoint : BaseFastEndpoint<GetWeatherForecasts.Query, GetWeatherForecasts.Response>
{
    public override void Configure()
    {
        Get(""api/weatherForecasts"");
    }

    public override async Task HandleAsync(GetWeatherForecasts.Query request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}";

        var sources = new[]
        {
            (typeof(ApiEndpointAttribute).Assembly.GetName().Name + ".cs", TestCode)
        };

        var references = new[]
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
        var generator = new FastEndpointSourceGenerator();

        // Get the driver that will apply the generator to the compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generator
        driver = driver.RunGenerators(compilation);
        
        // Use the driver to run the generators and get the results
        var runResult = driver.GetRunResult();

        // Get the generated files
        runResult.GeneratedTrees.Length.Should().Be(1);
        var actualGeneratedCode = runResult.GeneratedTrees[0].ToString();

        // Compare the generated code
        actualGeneratedCode.Should().Be(expectedGeneratedCode);
    }
}