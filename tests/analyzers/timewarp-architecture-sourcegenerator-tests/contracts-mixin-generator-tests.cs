namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using TimeWarp.Foundation.Contracts.Generators;

// Verifies the Roslyn generator that replaced the foundation-contracts Moxy mixins (task 053-001)
// reproduces the Moxy output semantics: marker attributes in the consumer RootNamespace, route
// members with the correct type mapping, and the two interface mixins.
public class ContractsMixinGenerator_Tests
{
  private const string Source = """
    namespace Test.Features.Admin.Roles;

    public static partial class GetRole
    {
        [RouteMixin("api/Roles/{RoleId:min(1)}", HttpVerb.Get)]
        public sealed partial class Query { }
    }

    public static partial class GetRoles
    {
        [IOpenDataQueryParametersMixin]
        [IAuthApiRequestMixin]
        public sealed partial class Query { }
    }
    """;

  private static string RunAndConcat(string rootNamespace)
  {
    var compilation = CSharpCompilation.Create(
      "Test.Contracts",
      new[] { CSharpSyntaxTree.ParseText(Source) },
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

  public static Task Should_Emit_Marker_Attributes_In_RootNamespace()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class RouteMixinAttribute : System.Attribute");
    generated.ShouldContain("public RouteMixinAttribute(string RouteTemplate, global::TimeWarp.Foundation.Features.HttpVerb HttpVerb)");
    generated.ShouldContain("internal sealed class IAuthApiRequestMixinAttribute : System.Attribute");
    generated.ShouldContain("internal sealed class IOpenDataQueryParametersMixinAttribute : System.Attribute");
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
}
