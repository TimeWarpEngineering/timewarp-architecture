namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Foundation.Contracts.Generators;

// Verifies the Roslyn generator that replaced the foundation-contracts Moxy mixins (task 053-001)
// reproduces the Moxy output semantics: marker attributes in the consumer RootNamespace, route
// members with the correct type mapping, and the two interface mixins. Task 053-003: bare
// `{Name}` tokens keep the full identifier (colon is required before a constraint).
public class ContractsMixinGenerator_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ContractsMixinGenerator_Tests>();

  private const string Source = """
    namespace Test.Features.Admin.Roles;

    public static partial class GetRole
    {
        [ApiRoute("api/Roles/{RoleId:min(1)}", HttpVerb.Get)]
        public sealed partial class Query { }
    }

    public static partial class GetRoles
    {
        [OpenDataQueryParameters]
        [AuthApiRequest]
        public sealed partial class Query { }
    }
    """;

  private static string Run(string source, string rootNamespace = "TimeWarp.Architecture")
  {
    var compilation = CSharpCompilation.Create(
      "Test.Contracts",
      new[] { CSharpSyntaxTree.ParseText(source) },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var options = new Dictionary<string, string> { ["build_property.RootNamespace"] = rootNamespace };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new ContractsMixinGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
  }

  private static string RunAndConcat(string rootNamespace) => Run(Source, rootNamespace);

  private static string RouteContract(string template) => $$"""
    namespace Test.Features.Ccc;

    public static partial class ValidateExport
    {
        [ApiRoute("{{template}}", HttpVerb.Post)]
        public sealed partial class Command { }
    }
    """;

  public static Task Should_Emit_Marker_Attributes_In_RootNamespace()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class ApiRouteAttribute : System.Attribute");
    generated.ShouldContain("public ApiRouteAttribute(string RouteTemplate, global::TimeWarp.Foundation.Features.HttpVerb HttpVerb)");
    generated.ShouldContain("internal sealed class AuthApiRequestAttribute : System.Attribute");
    generated.ShouldContain("internal sealed class OpenDataQueryParametersAttribute : System.Attribute");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Route_Members_With_Type_Mapping()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    // min(1) maps to int; both GetRoute overloads + the property are emitted.
    generated.ShouldContain("""public const string RouteTemplate = "api/Roles/{RoleId:min(1)}";""");
    generated.ShouldContain("GetHttpVerb() => global::TimeWarp.Foundation.Features.HttpVerb.Get;");
    generated.ShouldContain("""public string GetRoute(int RoleId) => global::System.FormattableString.Invariant($"api/Roles/{RoleId}");""");
    generated.ShouldContain("""public string GetRoute() => global::System.FormattableString.Invariant($"api/Roles/{RoleId}");""");
    generated.ShouldContain("public int RoleId { get; set; }");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Interface_Mixins()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain(": global::TimeWarp.Foundation.Features.IAuthApiRequest");
    generated.ShouldContain("public global::System.Guid UserId { get; set; }");
    generated.ShouldContain(": global::TimeWarp.Foundation.Features.IOpenDataQueryParameters");
    generated.ShouldContain("public int? Top { get; set; }");
    generated.ShouldContain("public bool ReturnTotalCount { get; set; }");
    return Task.CompletedTask;
  }

  public static Task Should_Keep_Bare_Param_Names_That_End_With_Type_Like_Letters()
  {
    string[] paramNames = ["LocationId", "Date", "ClientId", "StaffId", "UserId"];
    foreach (string paramName in paramNames)
    {
      string generated = Run(RouteContract($"api/items/{{{paramName}}}"));

      generated.ShouldContain($"public string {paramName} {{ get; set; }}");
      generated.ShouldContain($"public string GetRoute(string {paramName})");
      generated.ShouldContain($"public const string RouteTemplate = \"api/items/{{{paramName}}}\";");
      generated.ShouldNotContain("public e Dat");
      generated.ShouldNotContain("public d LocationI");
    }

    return Task.CompletedTask;
  }

  public static Task Should_Keep_Explicit_Constraints_On_Type_Like_Names()
  {
    string generated = Run(RouteContract("api/ccc/locations/{LocationId:guid}/exports/{Date:datetime}/clients/{ClientId:string}"));

    generated.ShouldContain("public Guid LocationId { get; set; }");
    generated.ShouldContain("public DateTime Date { get; set; }");
    generated.ShouldContain("public string ClientId { get; set; }");
    generated.ShouldContain("public string GetRoute(Guid LocationId, DateTime Date, string ClientId)");
    generated.ShouldContain("""public const string RouteTemplate = "api/ccc/locations/{LocationId:guid}/exports/{Date:datetime}/clients/{ClientId}";""");
    generated.ShouldContain("""public string GetRoute(Guid LocationId, DateTime Date, string ClientId) => global::System.FormattableString.Invariant($"api/ccc/locations/{LocationId}/exports/{Date:yyyy-MM-dd}/clients/{ClientId}");""");
    return Task.CompletedTask;
  }

  public static Task Should_Parse_Mixed_Bare_And_Constrained_Params()
  {
    // Crunchit 033-003 repro plus sibling *Id names in one template.
    string generated = Run(RouteContract("api/ccc/locations/{LocationId}/exports/{Date}/validate/{ClientId:guid}/{StaffId}/{UserId:string}"));

    generated.ShouldContain("public string LocationId { get; set; }");
    generated.ShouldContain("public string Date { get; set; }");
    generated.ShouldContain("public Guid ClientId { get; set; }");
    generated.ShouldContain("public string StaffId { get; set; }");
    generated.ShouldContain("public string UserId { get; set; }");
    generated.ShouldContain("public string GetRoute(string LocationId, string Date, Guid ClientId, string StaffId, string UserId)");
    generated.ShouldContain("""public const string RouteTemplate = "api/ccc/locations/{LocationId}/exports/{Date}/validate/{ClientId:guid}/{StaffId}/{UserId}";""");
    generated.ShouldNotContain("public d LocationI");
    generated.ShouldNotContain("public e Dat");
    return Task.CompletedTask;
  }
}
