#region Purpose
// Tests for TWPA0009: feature folders must not reference namespaces owned by other features.
#endregion

// ReSharper disable InconsistentNaming
namespace FeatureIsolationAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Feature_Isolation
{
  private const string AlphaSource =
    """
    namespace App.Features.Alphas;

    public class AlphaState
    {
      public static void Poke() { }
    }
    """;

  private static CSharpAnalyzerTest<FeatureIsolationAnalyzer, FixieVerifier> Test(params (string path, string source)[] files)
  {
    CSharpAnalyzerTest<FeatureIsolationAnalyzer, FixieVerifier> test = new();
    foreach ((string path, string source) in files)
    {
      test.TestState.Sources.Add((path, source));
    }

    return test;
  }

  public static async Task Given_CrossFeature_Reference_Flags()
  {
    const string BetaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<FeatureIsolationAnalyzer, FixieVerifier> test = Test(
      ("/src/features/alpha/alpha-state.cs", AlphaSource),
      ("/src/features/beta/beta-state.cs", BetaSource));

    // Both identifiers resolving into the owned namespace flag: the type and the invoked member.
    test.ExpectedDiagnostics.Add(
      new DiagnosticResult(id: "TWPA0009", DiagnosticSeverity.Warning)
        .WithSpan("/src/features/beta/beta-state.cs", 5, 49, 5, 59)
        .WithArguments("beta", "App.Features.Alphas", "alpha"));
    test.ExpectedDiagnostics.Add(
      new DiagnosticResult(id: "TWPA0009", DiagnosticSeverity.Warning)
        .WithSpan("/src/features/beta/beta-state.cs", 5, 60, 5, 64)
        .WithArguments("beta", "App.Features.Alphas", "alpha"));

    await test.RunAsync();
  }

  public static async Task Given_SameFeature_Reference_IsClean()
  {
    const string OtherAlphaSource =
      """
      namespace App.Features.Alphas;

      public class AlphaSibling
      {
        public void Use() => AlphaState.Poke();
      }
      """;

    await Test(
      ("/src/features/alpha/alpha-state.cs", AlphaSource),
      ("/src/features/alpha/alpha-sibling.cs", OtherAlphaSource)).RunAsync();
  }

  public static async Task Given_Shell_Reference_To_Feature_IsClean()
  {
    const string ShellSource =
      """
      namespace App.Components;

      public class NavShell
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("/src/features/alpha/alpha-state.cs", AlphaSource),
      ("/src/components/nav-shell.cs", ShellSource)).RunAsync();
  }

  public static async Task Given_Shared_Namespace_IsClean()
  {
    // Both features declare App.Pages — multi-owner namespaces carry no ownership.
    const string AlphaPage = "namespace App.Pages; public class AlphaPage { public static void Go() { } }";
    const string BetaPage = "namespace App.Pages; public class BetaPage { public void Use() => AlphaPage.Go(); }";

    await Test(
      ("/src/features/alpha/AlphaPage.cs", AlphaPage),
      ("/src/features/beta/BetaPage.cs", BetaPage)).RunAsync();
  }

  public static async Task Given_OptOut_Attribute_IsClean()
  {
    const string OptOutAttributeSource =
      """
      namespace App.Shared;

      [System.AttributeUsage(System.AttributeTargets.Class)]
      public sealed class CrossFeatureReferenceAttribute : System.Attribute
      {
        public CrossFeatureReferenceAttribute(string reason) { }
      }
      """;

    const string BetaSource =
      """
      namespace App.Features.Betas;

      [App.Shared.CrossFeatureReference("demo page deliberately exercises alpha")]
      public class BetaDemo
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("/src/shared/opt-out.cs", OptOutAttributeSource),
      ("/src/features/alpha/alpha-state.cs", AlphaSource),
      ("/src/features/beta/beta-demo.cs", BetaSource)).RunAsync();
  }

  public static async Task Given_Base_Substrate_Reference_IsClean()
  {
    const string BaseSource =
      """
      namespace App.Features;

      public class BaseState
      {
        public static void Poke() { }
      }
      """;

    const string BetaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void Use() => App.Features.BaseState.Poke();
      }
      """;

    await Test(
      ("/src/features/base/base-state.cs", BaseSource),
      ("/src/features/beta/beta-state.cs", BetaSource)).RunAsync();
  }
}
