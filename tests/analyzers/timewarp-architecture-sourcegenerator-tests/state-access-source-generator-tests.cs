namespace TimeWarp.Architecture.SourceGenerator.Tests;

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

// Verifies the Roslyn generator that replaced the web-spa StateAccessMixin Moxy mixin (task 053):
// for each [StateAccessMixin] state class it emits the typed accessors into the shared BaseComponent
// and BaseHandler<TAction> partials, plus the marker attribute in the RootNamespace.
public class StateAccessSourceGenerator_Tests
{
  private const string Source = """
    namespace Test.Features.Counters;

    [StateAccessMixin]
    public sealed class CounterState { }
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
      generators: ImmutableArray.Create(new StateAccessSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    return string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
  }

  public static Task Should_Emit_Marker_Attribute_In_RootNamespace()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("namespace TimeWarp.Architecture");
    generated.ShouldContain("internal sealed class StateAccessMixinAttribute : System.Attribute");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_BaseComponent_Accessors()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("namespace TimeWarp.Architecture.Features;");
    generated.ShouldContain("public partial class BaseComponent");
    generated.ShouldContain("internal global::Test.Features.Counters.CounterState CounterState => GetState<global::Test.Features.Counters.CounterState>();");
    generated.ShouldContain("internal global::Test.Features.Counters.CounterState NoSubCounterState => GetState<global::Test.Features.Counters.CounterState>(false);");
    return Task.CompletedTask;
  }

  public static Task Should_Emit_BaseHandler_Accessor()
  {
    string generated = RunAndConcat("TimeWarp.Architecture");

    generated.ShouldContain("internal abstract partial class BaseHandler<TAction>");
    generated.ShouldContain("protected global::Test.Features.Counters.CounterState CounterState => Store.GetState<global::Test.Features.Counters.CounterState>();");
    return Task.CompletedTask;
  }
}
