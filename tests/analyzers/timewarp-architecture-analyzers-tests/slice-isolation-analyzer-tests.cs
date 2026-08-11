#region Purpose
// Tests for TWA0009: namespace-based slice isolation (product/platform/substrate matrix).
#endregion

#region Design
// Ownership is path-independent: only namespaces under SliceRoot matter. RootNamespace is
// supplied via a global analyzer config file so tests do not depend on file paths.
#endregion

// ReSharper disable InconsistentNaming
namespace SliceIsolationAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Slice_Isolation
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Slice_Isolation>();

  private const string GlobalConfig =
    """
    is_global = true
    build_property.RootNamespace = App
    """;

  private const string AlphaSource =
    """
    namespace App.Features.Alphas;

    public class AlphaState
    {
      public static void Poke() { }
    }
    """;

  private const string OptOutAttributeSource =
    """
    namespace App.Shared;

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CrossSliceReferenceAttribute : System.Attribute
    {
      public System.Type TargetType { get; }
      public string Reason { get; }
      public CrossSliceReferenceAttribute(System.Type targetType, string reason)
      {
        TargetType = targetType;
        Reason = reason;
      }
    }
    """;

  private static CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> Test(params (string path, string source)[] files)
  {
    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", GlobalConfig));
    foreach ((string path, string source) in files)
    {
      test.TestState.Sources.Add((path, source));
    }

    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string sourceSlice, string referenced, string targetSlice) =>
    new DiagnosticResult(id: "TWA0009", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(sourceSlice, referenced, targetSlice);

  public static async Task Given_CrossProduct_Reference_Flags()
  {
    const string betaPath = "beta-state.cs";
    const string betaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      ("alpha-state.cs", AlphaSource),
      (betaPath, betaSource));

    // AlphaState type identifier and Poke member both resolve into the Alphas product slice.
    test.ExpectedDiagnostics.Add(Flag(betaPath, 5, 49, 59, "Betas", "AlphaState", "Alphas"));
    test.ExpectedDiagnostics.Add(Flag(betaPath, 5, 60, 64, "Betas", "AlphaState", "Alphas"));

    await test.RunAsync();
  }

  public static async Task Given_SameProduct_Reference_IsClean()
  {
    const string otherAlphaSource =
      """
      namespace App.Features.Alphas;

      public class AlphaSibling
      {
        public void Use() => AlphaState.Poke();
      }
      """;

    await Test(
      ("alpha-state.cs", AlphaSource),
      ("alpha-sibling.cs", otherAlphaSource)).RunAsync();
  }

  public static async Task Given_StructuralSuffix_SameProduct_IsClean()
  {
    const string pageSource =
      """
      namespace App.Features.Alphas.Pages;

      public class AlphaPage
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("alpha-state.cs", AlphaSource),
      ("alpha-page.cs", pageSource)).RunAsync();
  }

  public static async Task Given_Outside_Reference_To_Product_IsClean()
  {
    const string shellSource =
      """
      namespace App.Components;

      public class NavShell
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("alpha-state.cs", AlphaSource),
      ("nav-shell.cs", shellSource)).RunAsync();
  }

  public static async Task Given_Product_To_Substrate_IsClean()
  {
    const string baseSource =
      """
      namespace App.Features;

      public class BaseState
      {
        public static void Poke() { }
      }
      """;

    const string betaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void Use() => App.Features.BaseState.Poke();
      }
      """;

    await Test(
      ("base-state.cs", baseSource),
      ("beta-state.cs", betaSource)).RunAsync();
  }

  public static async Task Given_Product_To_Platform_IsClean()
  {
    const string platformSource =
      """
      namespace App.Features.Applications;

      public class ApplicationState
      {
        public static void Reset() { }
      }
      """;

    const string betaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void Use() => App.Features.Applications.ApplicationState.Reset();
      }
      """;

    await Test(
      ("application-state.cs", platformSource),
      ("beta-state.cs", betaSource)).RunAsync();
  }

  public static async Task Given_Platform_To_Product_Flags()
  {
    const string platformPath = "application-shell.cs";
    const string platformSource =
      """
      namespace App.Features.Applications;

      public class ApplicationShell
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      ("alpha-state.cs", AlphaSource),
      (platformPath, platformSource));

    test.ExpectedDiagnostics.Add(Flag(platformPath, 5, 44, 54, "Applications", "AlphaState", "Alphas"));
    test.ExpectedDiagnostics.Add(Flag(platformPath, 5, 55, 59, "Applications", "AlphaState", "Alphas"));

    await test.RunAsync();
  }

  public static async Task Given_Platform_To_Product_OptOut_IsClean()
  {
    const string platformSource =
      """
      namespace App.Features.Applications;

      [App.Shared.CrossSliceReference(typeof(App.Features.Alphas.AlphaState), "shell demo needs alpha")]
      public class ApplicationShell
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("opt-out.cs", OptOutAttributeSource),
      ("alpha-state.cs", AlphaSource),
      ("application-shell.cs", platformSource)).RunAsync();
  }

  public static async Task Given_Substrate_To_Product_Flags()
  {
    const string substratePath = "base-handler.cs";
    const string substrateSource =
      """
      namespace App.Features;

      public class BaseHandler
      {
        public void Use() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      ("alpha-state.cs", AlphaSource),
      (substratePath, substrateSource));

    test.ExpectedDiagnostics.Add(Flag(substratePath, 5, 44, 54, "Substrate", "AlphaState", "Alphas"));
    test.ExpectedDiagnostics.Add(Flag(substratePath, 5, 55, 59, "Substrate", "AlphaState", "Alphas"));

    await test.RunAsync();
  }

  public static async Task Given_Nested_AdminRoles_Are_Distinct_From_AdminOther()
  {
    const string rolesPath = "roles.cs";
    const string rolesSource =
      """
      namespace App.Features.Admin.Roles;

      public class RoleState
      {
        public static void Poke() { }
      }
      """;

    const string otherPath = "other.cs";
    const string otherSource =
      """
      namespace App.Features.Admin.Other;

      public class OtherState
      {
        public void Use() => App.Features.Admin.Roles.RoleState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      (rolesPath, rolesSource),
      (otherPath, otherSource));

    test.ExpectedDiagnostics.Add(Flag(otherPath, 5, 49, 58, "Admin.Other", "RoleState", "Admin.Roles"));
    test.ExpectedDiagnostics.Add(Flag(otherPath, 5, 59, 63, "Admin.Other", "RoleState", "Admin.Roles"));

    await test.RunAsync();
  }

  public static async Task Given_Metadata_Reference_IsClean()
  {
    // System.String is metadata — same-assembly filter skips it even from product code.
    const string betaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public string Use() => string.Empty;
      }
      """;

    await Test(("beta-state.cs", betaSource)).RunAsync();
  }

  public static async Task Given_EdgeScoped_OptOut_Suppresses_Only_Listed_Slice()
  {
    const string gammaSource =
      """
      namespace App.Features.Gammas;

      public class GammaState
      {
        public static void Poke() { }
      }
      """;

    const string betaPath = "beta-demo.cs";
    const string betaSource =
      """
      namespace App.Features.Betas;

      [App.Shared.CrossSliceReference(typeof(App.Features.Alphas.AlphaState), "demo exercises alpha")]
      public class BetaDemo
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
        public void UseGamma() => App.Features.Gammas.GammaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      ("opt-out.cs", OptOutAttributeSource),
      ("alpha-state.cs", AlphaSource),
      ("gamma-state.cs", gammaSource),
      (betaPath, betaSource));

    // Alpha opted out; Gamma still flags (two identifiers).
    test.ExpectedDiagnostics.Add(Flag(betaPath, 7, 49, 59, "Betas", "GammaState", "Gammas"));
    test.ExpectedDiagnostics.Add(Flag(betaPath, 7, 60, 64, "Betas", "GammaState", "Gammas"));

    await test.RunAsync();
  }

  public static async Task Given_Partial_OptOut_On_Other_Part_IsClean()
  {
    const string partA =
      """
      namespace App.Features.Betas;

      [App.Shared.CrossSliceReference(typeof(App.Features.Alphas.AlphaState), "split across partials")]
      public partial class BetaDemo
      {
      }
      """;

    const string partB =
      """
      namespace App.Features.Betas;

      public partial class BetaDemo
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    await Test(
      ("opt-out.cs", OptOutAttributeSource),
      ("alpha-state.cs", AlphaSource),
      ("beta-a.cs", partA),
      ("beta-b.cs", partB)).RunAsync();
  }

  public static async Task Given_Generic_Type_Argument_Flags()
  {
    const string betaPath = "beta-generic.cs";
    const string betaSource =
      """
      namespace App.Features.Betas;

      public class Box<T> { public static void Hold() { } }

      public class BetaState
      {
        public void Use() => Box<App.Features.Alphas.AlphaState>.Hold();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = Test(
      ("alpha-state.cs", AlphaSource),
      (betaPath, betaSource));

    // AlphaState appears as a generic type argument (SimpleNameSyntax).
    test.ExpectedDiagnostics.Add(Flag(betaPath, 7, 48, 58, "Betas", "AlphaState", "Alphas"));

    await test.RunAsync();
  }

  public static async Task Given_Missing_RootNamespace_NoOps()
  {
    const string betaSource =
      """
      namespace App.Features.Betas;

      public class BetaState
      {
        public void UseAlpha() => App.Features.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = new();
    // No AnalyzerConfigFiles → no RootNamespace → analyzer no-ops.
    test.TestState.Sources.Add(("alpha-state.cs", AlphaSource));
    test.TestState.Sources.Add(("beta-state.cs", betaSource));
    await test.RunAsync();
  }

  public static async Task Given_TimeWarpSliceRoot_Override_IsHonored()
  {
    const string overrideConfig =
      """
      is_global = true
      build_property.TimeWarpSliceRoot = App.Slices
      """;

    const string alpha =
      """
      namespace App.Slices.Alphas;

      public class AlphaState
      {
        public static void Poke() { }
      }
      """;

    const string betaPath = "beta.cs";
    const string beta =
      """
      namespace App.Slices.Betas;

      public class BetaState
      {
        public void Use() => App.Slices.Alphas.AlphaState.Poke();
      }
      """;

    CSharpAnalyzerTest<SliceIsolationAnalyzer, RoslynTestVerifier> test = new();
    test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", overrideConfig));
    test.TestState.Sources.Add(("alpha.cs", alpha));
    test.TestState.Sources.Add((betaPath, beta));
    test.ExpectedDiagnostics.Add(Flag(betaPath, 5, 42, 52, "Betas", "AlphaState", "Alphas"));
    test.ExpectedDiagnostics.Add(Flag(betaPath, 5, 53, 57, "Betas", "AlphaState", "Alphas"));
    await test.RunAsync();
  }
}
