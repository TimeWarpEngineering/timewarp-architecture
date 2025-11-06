// ReSharper disable InconsistentNaming
namespace PartialClassDeclarationAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzer;
using TimeWarp.Architecture.Analyzer.Tests;

public class Should_Trigger_PartialClassDeclaration
{
  public static async Task Given_PrimaryFileWithoutFullSpecifiers()
  {
    const string PrimaryFile =
      """
      partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string SecondaryFile =
      """
      partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWPA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.cs", startLine: 1, startColumn: 15, endLine: 1, endColumn: 31)
      .WithArguments("ApplicationState", "should have full specifiers in the primary file");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.Partial.cs", SecondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithExcessiveSpecifiers()
  {
    const string PrimaryFile =
      """
      public partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string SecondaryFile =
      """
      public partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWPA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.CloseModal.cs", startLine: 1, startColumn: 22, endLine: 1, endColumn: 38)
      .WithArguments("ApplicationState", "should have minimal specifiers in secondary files");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.CloseModal.cs", SecondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_IncorrectNamingConvention()
  {
    const string PrimaryFile =
      """
      public partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string IncorrectSecondaryFile =
      """
      partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWPA0001", DiagnosticSeverity.Warning)
      .WithSpan("WrongFileName.cs", startLine: 1, startColumn: 15, endLine: 1, endColumn: 31)
      .WithArguments("ApplicationState", "file name 'WrongFileName.cs' does not follow the expected naming convention");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("WrongFileName.cs", IncorrectSecondaryFile)
        }
      }
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
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.CloseModal.cs", SecondaryFile1),
          ("ApplicationState.ResetStore.cs", SecondaryFile2)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_KebabCaseFileNaming()
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
      TestState =
      {
        Sources =
        {
          ("application-state.cs", PrimaryFile),
          ("application-state.close-modal.cs", SecondaryFile1),
          ("application-state.reset-store.cs", SecondaryFile2)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithClassInheritance()
  {
    const string PrimaryFile =
      """
      public abstract class BaseApplicationState
      {
      }

      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string SecondaryFile =
      """
      partial class ApplicationState : BaseApplicationState
      {
          // Secondary content with class inheritance
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWPA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.Extensions.cs", startLine: 1, startColumn: 32, endLine: 1, endColumn: 54)
      .WithArguments("ApplicationState", "should not include class inheritance in secondary files");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.Extensions.cs", SecondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithInterfaceOnly()
  {
    const string PrimaryFile =
      """
      public interface IAnotherInterface
      {
      }

      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string SecondaryFile =
      """
      partial class ApplicationState : IAnotherInterface
      {
          // Secondary content with interface implementation
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", PrimaryFile),
          ("ApplicationState.Interfaces.cs", SecondaryFile)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SinglePartialDeclaration()
  {
    const string SingleFile =
      """
      partial class ApplicationState
      {
          // Only declaration
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, FixieVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", SingleFile)
        }
      }
    };

    await analyzerTest.RunAsync();
  }
}
