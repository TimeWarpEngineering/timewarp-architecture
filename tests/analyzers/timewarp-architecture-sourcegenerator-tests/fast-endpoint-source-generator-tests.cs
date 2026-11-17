#region Purpose
// Verifies FastEndpointSourceGenerator emits a FastEndpoint from a referenced [ApiEndpoint] contract:
// the enabled gate, feature tags, and the HTTP verb and request kind read from enum metadata.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_Tests>();

  // A minimal [ApiEndpoint] contract. The generator finds it by scanning REFERENCED assemblies
  // (cross-assembly, per task 007), so the harness compiles this into a separate assembly.
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
    // Default OpenAPI tag is the leaf namespace under Features (not the parent of Features).
    // Tags() is FE filter metadata; WithTags is what Scalar/OpenAPI group by.
    generatedCode.ShouldContain("""Tags("WeatherForecast")""");
    generatedCode.ShouldContain("""WithTags("WeatherForecast")""");
    // Task 110 fail-closed default: no [EndpointAuthorize]/[EndpointAllowAnonymous] marker means
    // the generator emits NOTHING — FastEndpoints then requires authentication by default. This is
    // the inverse of the pre-110 assertion (which used to require AllowAnonymous() here).
    generatedCode.ShouldNotContain("AllowAnonymous");

    return Task.CompletedTask;
  }

  private const string NestedAdminRolesContract = """
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

  public static Task Should_Tag_Nested_Feature_With_Leaf_Namespace()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(NestedAdminRolesContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    // Nested …Features.Admin.Roles → leaf "Roles", not "Admin" or "Architecture".
    generatedCode.ShouldContain("""Tags("Roles")""");
    generatedCode.ShouldContain("""WithTags("Roles")""");
    generatedCode.ShouldNotContain("""Tags("Admin")""");
    generatedCode.ShouldNotContain("""Tags("Architecture")""");

    return Task.CompletedTask;
  }

  public static Task Should_Not_Generate_When_Disabled()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    // EnableApiEndpointGeneration is not set (defaults to false).
    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: false);

    int generatedCount = runResult.Results.Sum(r => r.GeneratedSources.Length);
    generatedCount.ShouldBe(0);

    return Task.CompletedTask;
  }

  public static Task Should_Generate_When_Explicitly_Enabled()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(WeatherContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    int generatedCount = runResult.Results.Sum(r => r.GeneratedSources.Length);
    generatedCount.ShouldBe(1);

    return Task.CompletedTask;
  }

  // Post + Command: HttpVerb must resolve from enum metadata (not underlying int "1" → Get),
  // and BaseFastEndpoint must bind Command not Query.
  private const string CreateItemCommandContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Items;

    [ApiEndpoint]
    public static partial class CreateItem
    {
        [ApiRoute("api/items", HttpVerb.Post)]
        public sealed partial class Command
        {
            public string? Name { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Post_And_Command_For_Command_Contract()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(CreateItemCommandContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    ImmutableArray<GeneratedSourceResult> generated =
      runResult.Results.SelectMany(r => r.GeneratedSources).ToImmutableArray();
    generated.Length.ShouldBe(1);

    string generatedCode = generated[0].SourceText.ToString();
    generatedCode.ShouldContain("""Post("api/items")""");
    generatedCode.ShouldContain("BaseFastEndpoint<CreateItem.Command, CreateItem.Response>");
    generatedCode.ShouldNotContain("CreateItem.Query");
    generatedCode.ShouldNotContain("ExampleRequest");
    // Task 110 fail-closed default — see Should_Generate_Endpoint_From_Contract's comment.
    generatedCode.ShouldNotContain("AllowAnonymous");

    return Task.CompletedTask;
  }

  private const string DeleteItemContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Items;

    [ApiEndpoint]
    public static partial class DeleteItem
    {
        [ApiRoute("api/items/{id}", HttpVerb.Delete)]
        public sealed partial class Command
        {
            public string? Id { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Delete_Verb_From_Enum_Metadata()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(DeleteItemContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Delete("api/items/{id}")""");
    generatedCode.ShouldContain("BaseFastEndpoint<DeleteItem.Command, DeleteItem.Response>");

    return Task.CompletedTask;
  }

  private const string PutItemContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Items;

    [ApiEndpoint]
    public static partial class UpdateItem
    {
        [ApiRoute("api/items/{id}", HttpVerb.Put)]
        public sealed partial class Command
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
        }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Put_Verb_From_Enum_Metadata()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(PutItemContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Put("api/items/{id}")""");
    generatedCode.ShouldContain("BaseFastEndpoint<UpdateItem.Command, UpdateItem.Response>");

    return Task.CompletedTask;
  }
}
