// ReSharper disable InconsistentNaming
namespace PartialClassDeclarationAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Trigger_PartialClassDeclaration
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Trigger_PartialClassDeclaration>();

  public static async Task Given_PrimaryFileWithoutFullSpecifiers()
  {
    const string primaryFile =
      """
      partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string secondaryFile =
      """
      partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.cs", startLine: 1, startColumn: 15, endLine: 1, endColumn: 31)
      .WithArguments("ApplicationState", "should have full specifiers in the primary file");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("ApplicationState.Partial.cs", secondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithExcessiveSpecifiers()
  {
    const string primaryFile =
      """
      public partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string secondaryFile =
      """
      public partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.CloseModal.cs", startLine: 1, startColumn: 22, endLine: 1, endColumn: 38)
      .WithArguments("ApplicationState", "should have minimal specifiers in secondary files");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("ApplicationState.CloseModal.cs", secondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_IncorrectNamingConvention()
  {
    const string primaryFile =
      """
      public partial class ApplicationState
      {
          // Primary file content
      }
      """;

    const string incorrectSecondaryFile =
      """
      partial class ApplicationState
      {
          // Secondary file content
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWA0001", DiagnosticSeverity.Warning)
      .WithSpan("WrongFileName.cs", startLine: 1, startColumn: 15, endLine: 1, endColumn: 31)
      .WithArguments("ApplicationState", "file name 'WrongFileName.cs' does not follow the expected naming convention");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("WrongFileName.cs", incorrectSecondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_CorrectImplementation()
  {
    const string primaryFile =
      """
      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string secondaryFile1 =
      """
      partial class ApplicationState
      {
          // Secondary content 1
      }
      """;

    const string secondaryFile2 =
      """
      partial class ApplicationState
      {
          // Secondary content 2
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("ApplicationState.CloseModal.cs", secondaryFile1),
          ("ApplicationState.ResetStore.cs", secondaryFile2)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_KebabCaseFileNaming()
  {
    const string primaryFile =
      """
      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string secondaryFile1 =
      """
      partial class ApplicationState
      {
          // Secondary content 1
      }
      """;

    const string secondaryFile2 =
      """
      partial class ApplicationState
      {
          // Secondary content 2
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("application-state.cs", primaryFile),
          ("application-state.close-modal.cs", secondaryFile1),
          ("application-state.reset-store.cs", secondaryFile2)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithClassInheritance()
  {
    const string primaryFile =
      """
      public abstract class BaseApplicationState
      {
      }

      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string secondaryFile =
      """
      partial class ApplicationState : BaseApplicationState
      {
          // Secondary content with class inheritance
      }
      """;

    DiagnosticResult expectedDiagnostic = new DiagnosticResult(id: "TWA0001", DiagnosticSeverity.Warning)
      .WithSpan("ApplicationState.Extensions.cs", startLine: 1, startColumn: 32, endLine: 1, endColumn: 54)
      .WithArguments("ApplicationState", "should not include class inheritance in secondary files");

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("ApplicationState.Extensions.cs", secondaryFile)
        }
      }
    };

    analyzerTest.ExpectedDiagnostics.Add(expectedDiagnostic);

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SecondaryFileWithInterfaceOnly()
  {
    const string primaryFile =
      """
      public interface IAnotherInterface
      {
      }

      public partial class ApplicationState
      {
          // Primary content
      }
      """;

    const string secondaryFile =
      """
      partial class ApplicationState : IAnotherInterface
      {
          // Secondary content with interface implementation
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", primaryFile),
          ("ApplicationState.Interfaces.cs", secondaryFile)
        }
      }
    };

    await analyzerTest.RunAsync();
  }

  public static async Task Given_SinglePartialDeclaration()
  {
    const string singleFile =
      """
      partial class ApplicationState
      {
          // Only declaration
      }
      """;

    var analyzerTest = new CSharpAnalyzerTest<PartialClassDeclarationAnalyzer, RoslynTestVerifier>
    {
      TestState =
      {
        Sources =
        {
          ("ApplicationState.cs", singleFile)
        }
      }
    };

    await analyzerTest.RunAsync();
  }
}
