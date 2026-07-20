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
    generatedCode.ShouldContain("AllowAnonymous()");

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

public class FastEndpointSourceGenerator_Authorization_Tests
{
  private const string PolicyAuthorizedContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Policy = "AdminOnly")]
    public static partial class GetAdminStats
    {
        [ApiRoute("api/admin/stats", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Policies_For_EndpointAuthorize_Policy()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(PolicyAuthorizedContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Policies("AdminOnly")""");
    generatedCode.ShouldNotContain("AllowAnonymous");
    generatedCode.ShouldNotContain("RequireAuthorization");

    return Task.CompletedTask;
  }

  private const string RolesAuthorizedContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin;

    [ApiEndpoint]
    [EndpointAuthorize(Roles = "Admin,Manager")]
    public static partial class GetTeamRoster
    {
        [ApiRoute("api/admin/roster", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_Roles_For_EndpointAuthorize_Roles()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(RolesAuthorizedContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""Roles("Admin", "Manager")""");
    generatedCode.ShouldNotContain("AllowAnonymous");

    return Task.CompletedTask;
  }

  private const string SchemesOnlyContract = """
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Agents;

    [ApiEndpoint]
    [EndpointAuthorize(AuthenticationSchemes = "Bearer")]
    public static partial class GetAgentIdentity
    {
        [ApiRoute("api/agents/me", HttpVerb.Get)]
        public sealed partial class Query { }

        public sealed class Response { }
    }
    """;

  public static Task Should_Emit_AuthSchemes_Without_AllowAnonymous()
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(SchemesOnlyContract);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(contract, enabled: true);

    string generatedCode = runResult.Results
      .SelectMany(r => r.GeneratedSources)
      .Single()
      .SourceText.ToString();

    generatedCode.ShouldContain("""AuthSchemes("Bearer")""");
    generatedCode.ShouldNotContain("AllowAnonymous");
    generatedCode.ShouldNotContain("RequireAuthorization");

    return Task.CompletedTask;
  }
}
