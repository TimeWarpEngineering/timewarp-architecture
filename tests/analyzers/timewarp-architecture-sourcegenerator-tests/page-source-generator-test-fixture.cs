#region Purpose
// Shared arrange helpers for the PageSourceGenerator test classes: runs the generator over a
// source snippet and slices the generated PageRegistry out of the output.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

internal static class PageSourceGeneratorTestFixture
{
  internal const string RootNamespace = "TimeWarp.Architecture";

  internal static (string Generated, ImmutableArray<Diagnostic> Diagnostics) Run(string source)
  {
    var compilation = CSharpCompilation.Create(
      "Test.WebSpa",
      new[] { CSharpSyntaxTree.ParseText(source) },
      new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var options = new Dictionary<string, string> { ["build_property.RootNamespace"] = RootNamespace };

    GeneratorDriver driver = CSharpGeneratorDriver.Create(
      generators: ImmutableArray.Create(new PageSourceGenerator().AsSourceGenerator()),
      optionsProvider: new TestAnalyzerConfigOptionsProvider(options));

    GeneratorDriverRunResult result = driver.RunGenerators(compilation).GetRunResult();
    string generated = string.Join(
      Environment.NewLine,
      result.Results.SelectMany(r => r.GeneratedSources).Select(s => s.SourceText.ToString()));
    ImmutableArray<Diagnostic> diagnostics = result.Diagnostics
      .Concat(result.Results.SelectMany(r => r.Diagnostics))
      .Distinct()
      .ToImmutableArray();
    return (generated, diagnostics);
  }

  internal static string RegistryOf(string generated)
  {
    int start = generated.IndexOf("public static class PageRegistry", StringComparison.Ordinal);
    start.ShouldBeGreaterThanOrEqualTo(0, "PageRegistry was not generated");
    return generated.Substring(start);
  }
}
