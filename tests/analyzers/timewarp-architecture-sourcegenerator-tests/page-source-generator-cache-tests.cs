#region Purpose
// Verifies page/registry outputs stay cached across unrelated edits (value-equatable models).
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static TimeWarp.Architecture.SourceGenerator.Tests.PageSourceGeneratorTestFixture;

public class PageSourceGenerator_Cache_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PageSourceGenerator_Cache_Tests>();

  public static Task Should_Cache_Page_And_Registry_Outputs_When_Unrelated_Tree_Is_Added()
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[]
      {
        CSharpSyntaxTree.ParseText("""
          namespace Test.Pages;
          [Page("/Counter", Navigable = true)]
          public partial class CounterPage { }
          [Page("/todoitems/{TodoItemId:Guid}", Navigable = true)]
          public partial class TodoItemPage { }
          [Page("/clients", "/clients/revenue", "/clients/{ClientId}/revenue", Navigable = true)]
          public partial class ClientsPage { }
          [Page("/a", "/A")]
          public partial class DuplicatePage { }
          [Page("/tabs", "/tabs/one")]
          public partial class TabsPage { }
          """),
      },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new PageSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(
        new Dictionary<string, string> { ["build_property.RootNamespace"] = RootNamespace }),
      driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    driver = driver.RunGenerators(compilation);

    compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
      namespace Unrelated;
      [System.Obsolete]
      public sealed class Other { }
      """));
    driver = driver.RunGenerators(compilation);

    GeneratorRunResult run = driver.GetRunResult().Results.Single();
    IncrementalStepRunReason[] reasons =
    [
      .. run.TrackedOutputSteps
        .SelectMany(static pair => pair.Value)
        .SelectMany(static step => step.Outputs)
        .Select(static output => output.Reason),
    ];

    reasons.ShouldNotBeEmpty();
    reasons.ShouldAllBe(static r => r == IncrementalStepRunReason.Cached || r == IncrementalStepRunReason.Unchanged);
    run.Diagnostics.ShouldContain(static d => d.Id == "TWE009");
    run.Diagnostics.ShouldContain(static d => d.Id == "TWE010");

    // Editing a multi-route page's alias string must invalidate its model and emit the new route.
    SyntaxTree original = compilation.SyntaxTrees.ToList()[0];
    SyntaxTree edited = original.WithChangedText(
      Microsoft.CodeAnalysis.Text.SourceText.From(original.ToString().Replace("\"/tabs/one\"", "\"/tabs/two\"", StringComparison.Ordinal)));
    compilation = compilation.ReplaceSyntaxTree(original, edited);
    driver = driver.RunGenerators(compilation);

    GeneratorRunResult editedRun = driver.GetRunResult().Results.Single();
    string editedOutput = string.Join(
      Environment.NewLine,
      editedRun.GeneratedSources.Select(static s => s.SourceText.ToString()));
    editedOutput.ShouldContain("[Route(\"/tabs/two\")]");
    editedOutput.ShouldNotContain("/tabs/one");
    editedRun.TrackedOutputSteps
      .SelectMany(static pair => pair.Value)
      .SelectMany(static step => step.Outputs)
      .Select(static output => output.Reason)
      .ShouldContain(static r => r != IncrementalStepRunReason.Cached && r != IncrementalStepRunReason.Unchanged);
    return Task.CompletedTask;
  }
}
