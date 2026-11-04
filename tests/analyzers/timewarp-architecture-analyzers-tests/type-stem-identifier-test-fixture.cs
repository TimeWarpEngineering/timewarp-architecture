#region Purpose
// Shared harness (stubs, config, Test and Flag builders) for the TWA0023 type-stem identifier tests.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

internal static class TypeStemIdentifierTestFixture
{
  internal const string EnableTwa0023GlobalConfig =
    """
    is_global = true
    dotnet_diagnostic.TWA0023.severity = warning
    """;

  internal const string AttributeStub =
    """
    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Parameter,
        AllowMultiple = false,
        Inherited = false)]
      public sealed class TypeStemIdentifierAttribute : System.Attribute
      {
        public string Reason { get; }
        public TypeStemIdentifierAttribute(string reason) => Reason = reason;
      }
    }
    """;

  internal static CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> Test(string source)
  {
    CSharpAnalyzerTest<TypeStemIdentifierAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", EnableTwa0023GlobalConfig));
    test.TestState.Sources.Add(("TypeStemIdentifierAttribute.cs", AttributeStub));
    test.TestState.Sources.Add(("Feature.cs", source));
    return test;
  }

  internal static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string identifier, string stem) =>
    new DiagnosticResult(id: "TWA0023", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(identifier, stem);
}
