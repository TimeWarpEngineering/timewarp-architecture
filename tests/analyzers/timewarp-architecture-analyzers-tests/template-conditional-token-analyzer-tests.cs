#region Purpose
// Tests for TWPA0008: template-conditional tokens in comments/strings flag; real directives and
// cnd:noEmit-escaped regions stay clean.
#endregion

#region Design
// This file is itself template content, so the directive tokens and cnd:noEmit markers in the
// embedded sample sources are COMPOSED at runtime — the raw byte sequences must never appear in
// this file or the dotnet-new engine would mangle it at generation (the exact bug class under
// test). Expected spans are computed from the composed lines rather than hand-counted.
#endregion

// ReSharper disable InconsistentNaming
namespace TemplateConditionalTokenAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Flag_Template_Conditional_Tokens
{
  // Composed so the raw sequences never appear in this file (see Design).
  private const string If = "#" + "if";
  private const string Endif = "#" + "endif";
  private const string DisableLine = "//" + "-:cnd:noEmit";
  private const string EnableLine = "//" + "+:cnd:noEmit";

  private static CSharpAnalyzerTest<TemplateConditionalTokenAnalyzer, FixieVerifier> Test(string[] lines, params DiagnosticResult[] expected)
  {
    CSharpAnalyzerTest<TemplateConditionalTokenAnalyzer, FixieVerifier> test =
      new() { TestState = { Sources = { ("Sample.cs", string.Join("\n", lines)) } } };
    test.ExpectedDiagnostics.AddRange(expected);
    return test;
  }

  private static DiagnosticResult Hit(string[] lines, int lineNumber, string token)
  {
    int column = lines[lineNumber - 1].IndexOf(token, System.StringComparison.Ordinal) + 1;
    return new DiagnosticResult(id: "TWPA0008", DiagnosticSeverity.Warning)
      .WithSpan("Sample.cs", lineNumber, column, lineNumber, column + token.Length)
      .WithArguments(token.Substring(1));
  }

  public static async Task Given_Token_In_Comment_Flags()
  {
    string[] lines =
    [
      "public class C1 { }",
      $"// prose about {If} false behavior",
    ];

    await Test(lines, Hit(lines, 2, If)).RunAsync();
  }

  public static async Task Given_Token_In_String_Flags()
  {
    string[] lines =
    [
      "public class C2",
      "{",
      $"  public const string S = \"has {If} inside\";",
      "}",
    ];

    await Test(lines, Hit(lines, 3, If)).RunAsync();
  }

  public static async Task Given_Token_In_Raw_String_Flags()
  {
    string[] lines =
    [
      "public class C3",
      "{",
      "  public const string S = \"\"\"",
      $"    sample with {Endif} token",
      "    \"\"\";",
      "}",
    ];

    await Test(lines, Hit(lines, 4, Endif)).RunAsync();
  }

  public static async Task Given_Token_In_Interpolated_String_Flags()
  {
    string[] lines =
    [
      "public class C4",
      "{",
      $"  public string T => $\"has {If} near {{1 + 1}}\";",
      "}",
    ];

    await Test(lines, Hit(lines, 3, If)).RunAsync();
  }

  public static async Task Given_Token_In_Doc_Comment_Flags()
  {
    string[] lines =
    [
      $"/// <summary>About the {If} directive.</summary>",
      "public class C5 { }",
    ];

    await Test(lines, Hit(lines, 1, If)).RunAsync();
  }

  public static async Task Given_Real_Directive_IsClean()
  {
    string[] lines =
    [
      $"{If} SOME_SYMBOL",
      "public class C6 { }",
      $"{Endif}",
      "public class C7 { }",
    ];

    await Test(lines).RunAsync();
  }

  public static async Task Given_NoEmit_Escaped_Token_IsClean()
  {
    string[] lines =
    [
      DisableLine,
      $"// prose about {If} false behavior",
      EnableLine,
      "public class C8 { }",
    ];

    await Test(lines).RunAsync();
  }

  public static async Task Given_Unclosed_NoEmit_Escapes_To_End_Of_File()
  {
    string[] lines =
    [
      "public class C9 { }",
      DisableLine,
      $"// prose about {Endif} behavior",
    ];

    await Test(lines).RunAsync();
  }

  public static async Task Given_Token_After_Closed_Escape_Flags()
  {
    string[] lines =
    [
      DisableLine,
      $"// exempt {If} here",
      EnableLine,
      $"// not exempt {If} here",
      "public class C10 { }",
    ];

    await Test(lines, Hit(lines, 4, If)).RunAsync();
  }
}
