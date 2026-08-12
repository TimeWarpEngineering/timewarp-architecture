#region Purpose
// Tests for TWA0010: template-flag directives without a matching DefineConstants entry flag;
// defined flags, non-template symbols, and missing template.json stay clean.
#endregion

#region Design
// Directive lines in the embedded sample sources are COMPOSED at runtime (the 087 lesson: the
// dotnet-new engine processes real conditional directives in template content — including this
// test file — and would strip or mangle them at generation). The fake template.json uses made-up
// flag names so no real template symbol appears here either.
#endregion

// ReSharper disable InconsistentNaming
namespace TemplateFlagConstantsAnalyzer_;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Require_Constants_For_Template_Flags
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Require_Constants_For_Template_Flags>();

  // Composed so raw directive tokens never appear in this template file (TWA0008 discipline).
#pragma warning disable RCS1190 // Join string expressions
  private const string If = "#" + "if";
  private const string Endif = "#" + "endif";
#pragma warning restore RCS1190

  private const string FakeTemplateJson =
    """
    {
      "symbols": {
        "alpha": { "type": "parameter", "datatype": "bool", "defaultValue": "true" },
        "beta": { "type": "parameter", "datatype": "bool", "defaultValue": "true" },
        "name": { "type": "parameter", "datatype": "string" }
      }
    }
    """;

  private static CSharpAnalyzerTest<TemplateFlagConstantsAnalyzer, RoslynTestVerifier> Test(
    string[] lines,
    string[]? definedSymbols = null,
    bool includeTemplateJson = true)
  {
    CSharpAnalyzerTest<TemplateFlagConstantsAnalyzer, RoslynTestVerifier> test =
      new() { TestState = { Sources = { ("Sample.cs", string.Join("\n", lines)) } } };

    if (includeTemplateJson)
    {
      test.TestState.AdditionalFiles.Add(("template.json", FakeTemplateJson));
    }

    if (definedSymbols is { Length: > 0 })
    {
      test.SolutionTransforms.Add((solution, projectId) =>
        solution.WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: definedSymbols)));
    }

    return test;
  }

  private static DiagnosticResult Hit(string[] lines, int lineNumber, string flag)
  {
    int column = lines[lineNumber - 1].IndexOf(flag, System.StringComparison.Ordinal) + 1;
    return new DiagnosticResult(id: "TWA0010", DiagnosticSeverity.Warning)
      .WithSpan("Sample.cs", lineNumber, column, lineNumber, column + flag.Length)
      .WithArguments(flag);
  }

  public static async Task Given_Flag_Directive_Without_Constant_Flags()
  {
    string[] lines =
    [
      $"{If}(alpha)",
      "public class C1 { }",
      Endif,
    ];

    CSharpAnalyzerTest<TemplateFlagConstantsAnalyzer, RoslynTestVerifier> test = Test(lines);
    test.ExpectedDiagnostics.Add(Hit(lines, 1, "alpha"));
    await test.RunAsync();
  }

  public static async Task Given_Flag_Directive_With_Constant_IsClean()
  {
    string[] lines =
    [
      $"{If}(alpha)",
      "public class C2 { }",
      Endif,
    ];

    await Test(lines, definedSymbols: ["alpha"]).RunAsync();
  }

  public static async Task Given_Compound_Condition_Flags_Only_Missing()
  {
    string[] lines =
    [
      $"{If}(alpha && beta)",
      "public class C3 { }",
      Endif,
    ];

    CSharpAnalyzerTest<TemplateFlagConstantsAnalyzer, RoslynTestVerifier> test = Test(lines, definedSymbols: ["alpha"]);
    test.ExpectedDiagnostics.Add(Hit(lines, 1, "beta"));
    await test.RunAsync();
  }

  public static async Task Given_NonTemplate_Symbol_IsClean()
  {
    string[] lines =
    [
      $"{If} SOME_LOCAL_SYMBOL",
      "public class C4 { }",
      Endif,
    ];

    await Test(lines).RunAsync();
  }

  public static async Task Given_No_TemplateJson_IsSilent()
  {
    string[] lines =
    [
      $"{If}(alpha)",
      "public class C5 { }",
      Endif,
    ];

    await Test(lines, includeTemplateJson: false).RunAsync();
  }

  public static async Task Given_NonBool_Symbol_IsClean()
  {
    // "name" is a string symbol in the fake template.json — not a flag.
    string[] lines =
    [
      $"{If}(name)",
      "public class C6 { }",
      Endif,
    ];

    await Test(lines).RunAsync();
  }
}
