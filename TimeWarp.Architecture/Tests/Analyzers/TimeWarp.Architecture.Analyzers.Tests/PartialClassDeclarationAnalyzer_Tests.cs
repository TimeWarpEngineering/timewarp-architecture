﻿// ReSharper disable InconsistentNaming
namespace PartialClassDeclarationAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzer;
using TimeWarp.Architecture.Analyzer.Tests;

public class Should_Trigger_PartialClassDeclaration
{
  public static async Task Given_PrimaryFileWithoutFullSpecifiers()
  {
    const string TestCode =
      """
      partial class ApplicationState
      {
          // Primary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "PartialClassDeclaration", DiagnosticSeverity.Warning)
      .WithSpan(startLine: 1, startColumn: 15, endLine: 1, endColumn: 32)
      .WithArguments("ApplicationState", "should have full specifiers in the primary file");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestCode = TestCode,
      TestState = { AdditionalFiles = { (filename: "ApplicationState.cs", TestCode) } }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithExcessiveSpecifiers()
  {
    const string TestCode =
      """
      public partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "PartialClassDeclaration", DiagnosticSeverity.Warning)
      .WithSpan(startLine: 1, startColumn: 22, endLine: 1, endColumn: 39)
      .WithArguments("ApplicationState", "should have minimal specifiers in secondary files");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestCode = TestCode,
      TestState = { AdditionalFiles = { ("ApplicationState.CloseModal.cs", TestCode) } }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_IncorrectNamingConvention()
  {
    const string TestCode =
      """
      partial class ApplicationState
      {
          // Content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "PartialClassDeclaration", DiagnosticSeverity.Warning)
      .WithSpan(startLine: 1, startColumn: 15, endLine: 1, endColumn: 32)
      .WithArguments("ApplicationState", "file name 'WrongFileName.cs' does not follow the expected naming convention");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestCode = TestCode,
      TestState = { AdditionalFiles = { (filename: "WrongFileName.cs", TestCode) } }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_CorrectImplementation()
  {
    const string PrimaryFile =
      """
      public partial class ApplicationState
      {
          // Primary content
      }
      """;
    const string SecondaryFile1 =
      """
      partial class ApplicationState
      {
          // Secondary content 1
      }
      """;
    const string SecondaryFile2 =
      """
      partial class ApplicationState
      {
          // Secondary content 2
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestCode = PrimaryFile + SecondaryFile1 + SecondaryFile2,
      TestState =
      {
        AdditionalFiles =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.CloseModal.cs", SecondaryFile1),
          ("ApplicationState.ResetStore.cs", SecondaryFile2)
        }
      }
    };

    await analyzerTest.RunAsync();
  }
}
