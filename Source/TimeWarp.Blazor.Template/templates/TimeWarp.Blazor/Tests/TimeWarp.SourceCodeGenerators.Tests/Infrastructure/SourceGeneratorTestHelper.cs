namespace TimeWarp.SourceCodeGenerators.Tests.Infrastructure
{
  using FluentAssertions;
  using Microsoft.CodeAnalysis;
  using Microsoft.CodeAnalysis.CSharp;
  using System;
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Linq;
  using TimeWarp.Blazor.Testing;

  [NotTest]
  public static class SourceGeneratorTestHelper
  {
    public static string GetGeneratedOutput<TGenerator>(string sourceCode)
      where TGenerator : class, ISourceGenerator, new()
    {
      SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
      IEnumerable<MetadataReference> references =
        AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => !assembly.IsDynamic)
        .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
        .Cast<MetadataReference>();

      var compilation =
        CSharpCompilation
        .Create
        (
          assemblyName: "SourceGeneratorTests",
          syntaxTrees: new[] { syntaxTree },
          references: references,
          options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

      var generator = new TGenerator();
      CSharpGeneratorDriver
        .Create(generator)
        .RunGeneratorsAndUpdateCompilation
        (
          compilation,
          out Compilation outputCompilation,
          out ImmutableArray<Diagnostic> diagnostics
        );

      // optional
      diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

      return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString();
    }
  }
}


// References: https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/
