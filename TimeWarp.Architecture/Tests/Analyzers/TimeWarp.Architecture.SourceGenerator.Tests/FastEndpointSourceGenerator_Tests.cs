namespace TimeWarp.Architecture.SourceGenerator.Tests;

public class FastEndpointSourceGenerator_Tests
{
    public static Task Should_Generate_Endpoint_From_Contract()
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

        const string ExpectedGeneratedCode = @"
using FastEndpoints;
using OneOf;
using System.Threading;
using System.Threading.Tasks;

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
    }";

        (string, string)[] sources = new[]
        {
            (typeof(ApiEndpointAttribute).Assembly.GetName().Name + ".cs", TestCode)
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
        var generator = new FastEndpointSourceGenerator();

        // Get the driver that will apply the generator to the compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generator and get results
        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        // Get the generated files
        runResult.Results[0].GeneratedSources.Length.Should().Be(1);
        string actualGeneratedCode = runResult.Results[0].GeneratedSources[0].SourceText.ToString();

        // Compare the generated code
        actualGeneratedCode.Should().Be(ExpectedGeneratedCode);

        return Task.CompletedTask;
    }
}