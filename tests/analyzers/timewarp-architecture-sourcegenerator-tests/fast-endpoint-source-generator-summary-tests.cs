#region Purpose
// FastEndpoint generator OpenAPI summary order: request XML summary, then [Operation]
// Description, then outer-class XML summary, then empty (task 472).
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System.Linq;

public class FastEndpointSourceGenerator_Summary_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FastEndpointSourceGenerator_Summary_Tests>();

  // The host must reference TimeWarp.Contracts too, or [Operation] reads as an error type.
  private static readonly MetadataReference OperationAttributeReference =
    MetadataReference.CreateFromFile(typeof(TimeWarp.Contracts.OperationAttribute).Assembly.Location);

  private const string RequestSummary = "/// <summary>Request summary.</summary>";
  private const string OuterSummary = "/// <summary>Outer summary.</summary>";
  private const string OperationWithDescription =
    "[TimeWarp.Contracts.Operation(\"roles.set-permissions\", Description = \"Operation description.\")]";

  public static Task Should_Use_Request_Summary_When_It_Is_The_Only_Source()
  {
    string generated = Generate(Contract(outerDoc: "", operation: "", requestDoc: RequestSummary));

    generated.ShouldContain("s.Summary = \"Request summary.\";");
    return Task.CompletedTask;
  }

  public static Task Should_Use_Operation_Description_When_It_Is_The_Only_Source()
  {
    string generated = Generate(Contract(outerDoc: "", operation: OperationWithDescription, requestDoc: ""));

    generated.ShouldContain("s.Summary = \"Operation description.\";");
    return Task.CompletedTask;
  }

  public static Task Should_Use_Outer_Class_Summary_When_It_Is_The_Only_Source()
  {
    string generated = Generate(Contract(outerDoc: OuterSummary, operation: "", requestDoc: ""));

    generated.ShouldContain("s.Summary = \"Outer summary.\";");
    generated.ShouldContain("/// Outer summary.");
    return Task.CompletedTask;
  }

  public static Task Should_Prefer_Request_Summary_Over_Operation_And_Outer_Class()
  {
    string generated = Generate(Contract(OuterSummary, OperationWithDescription, RequestSummary));

    generated.ShouldContain("s.Summary = \"Request summary.\";");
    generated.ShouldNotContain("Operation description.");
    generated.ShouldNotContain("Outer summary.");
    return Task.CompletedTask;
  }

  public static Task Should_Prefer_Operation_Description_Over_Outer_Class_Summary()
  {
    // The Roles contracts shape: outer-class summary plus [Operation(Description)], no request docs.
    string generated = Generate(Contract(OuterSummary, OperationWithDescription, requestDoc: ""));

    generated.ShouldContain("s.Summary = \"Operation description.\";");
    generated.ShouldNotContain("Outer summary.");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_No_Summary_When_No_Source_Exists()
  {
    string generated = Generate(Contract(outerDoc: "", operation: "", requestDoc: ""));

    generated.ShouldNotContain("s.Summary =");
    generated.ShouldNotContain("Summary(s =>");
    return Task.CompletedTask;
  }

  public static Task Should_Read_Outer_Class_Summary_And_Remarks_From_Additional_Documentation()
  {
    // csc path: the reference carries no documentation provider; the .xml AdditionalFile does.
    string source = Contract(
      "/// <summary>Outer summary.</summary>\n/// <remarks>Outer remarks.</remarks>",
      operation: "",
      requestDoc: "");
    (MetadataReference reference, string documentationXml) = GeneratorTestHarness.CompileContractAssembly(
      source,
      "Test.Contracts",
      stampMarker: true,
      attachDocumentation: false);

    GeneratorDriverRunResult runResult = GeneratorTestHarness.Run(
      [reference, OperationAttributeReference],
      enabled: true,
      contractAssemblies: "Test.Contracts",
      additionalTexts: [new InMemoryAdditionalText("Test.Contracts.xml", documentationXml)]);

    string generated = SingleEndpoint(runResult);
    generated.ShouldContain("s.Summary = \"Outer summary.\";");
    generated.ShouldContain("s.Description = \"Outer remarks.\";");
    return Task.CompletedTask;
  }

  public static Task Should_Join_A_Wrapped_Summary_Into_One_Line()
  {
    string generated = Generate(Contract(
      "/// <summary>\n/// Replace stored permission ids\n/// for a role &amp; keep the core.\n/// </summary>",
      operation: "",
      requestDoc: ""));

    generated.ShouldContain("s.Summary = \"Replace stored permission ids for a role & keep the core.\";");
    generated.ShouldContain("/// Replace stored permission ids for a role &amp; keep the core.");
    return Task.CompletedTask;
  }

  private static string Contract(string outerDoc, string operation, string requestDoc) => $$"""
    using TimeWarp.Architecture;
    using TimeWarp.Architecture.Attributes;

    namespace Test.Features.Admin.Roles;

    {{outerDoc}}
    [ApiEndpoint]
    public static partial class SetRolePermissions
    {
        {{requestDoc}}
        [ApiRoute("api/Roles/{RoleId}/permissions", HttpVerb.Put)]
        {{operation}}
        public sealed partial class Command { public string? RoleId { get; set; } }

        public sealed class Response { }
    }
    """;

  private static string Generate(string contractSource)
  {
    MetadataReference contract = GeneratorTestHarness.CompileContractAssembly(contractSource);
    return SingleEndpoint(GeneratorTestHarness.Run([contract, OperationAttributeReference], enabled: true, "Test.Contracts"));
  }

  private static string SingleEndpoint(GeneratorDriverRunResult runResult)
    => runResult.Results
      .SelectMany(static result => result.GeneratedSources)
      .Single(static source => source.HintName.Contains("SetRolePermissions", StringComparison.Ordinal))
      .SourceText.ToString();
}
