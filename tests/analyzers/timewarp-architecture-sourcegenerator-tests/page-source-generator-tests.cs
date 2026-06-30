namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

// Verifies the Roslyn generator that replaced the web-spa Page Moxy mixin (task 053, last mixin):
// [Page("/route")] on a Blazor page emits the [Route] attribute, INavigableComponent/IStaticRoute,
// a static GetPageUrl(...), the Policy accessor, and [Parameter] props for route tokens.
public class PageSourceGenerator_Tests
{
  private const string Source = """
    namespace Test.Pages;

    [Page("/Counter")]
    public partial class CounterPage { }

    [Page("/todoitems/{TodoItemId:Guid}")]
    public partial class TodoItemPage { }
    """;

  private static string RunAndConcat(string rootNamespace)
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[] { CSharpSyntaxTree.ParseText(Source) },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var options = new Dictionary<string, string> { ["build_property.RootNamespace"] = rootNamespace };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new PageSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
  }

  public static Task Should_Emit_PageAttribute_In_RootNamespace()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class PageAttribute : System.Attribute");
    generated.ShouldContain("public PageAttribute(string RouteTemplate)");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Static_Route_Page()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    // No route params -> IStaticRoute + a no-arg GetPageUrl, Policy defaults to Anonymous.
    generated.ShouldContain("[Route(\"/Counter\")]");
    generated.ShouldContain("partial class CounterPage : INavigableComponent, IStaticRoute");
    generated.ShouldContain("public static string GetPageUrl() => global::System.FormattableString.Invariant($\"/Counter\");");
    generated.ShouldContain("public static string Policy { get; } = Policies.Anonymous;");
    return Task.CompletedTask;
  }

  public static Task Should_Generate_Parameterized_Page()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    // Route token {TodoItemId:Guid}: lowercased in [Route], real C# type in the signature + [Parameter].
    generated.ShouldContain("[Route(\"/todoitems/{TodoItemId:guid}\")]");
    generated.ShouldContain("partial class TodoItemPage : INavigableComponent");
    generated.ShouldNotContain("partial class TodoItemPage : INavigableComponent, IStaticRoute");
    generated.ShouldContain("public static string GetPageUrl(Guid TodoItemId) => global::System.FormattableString.Invariant($\"/todoitems/{TodoItemId}\");");
    generated.ShouldContain("[Parameter] public Guid TodoItemId { get; set; }");
    return Task.CompletedTask;
  }
}
