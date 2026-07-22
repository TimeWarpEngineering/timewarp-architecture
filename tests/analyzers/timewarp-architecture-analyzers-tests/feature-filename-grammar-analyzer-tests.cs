#region Purpose
// Tests for TWA0015/TWA0016 feature filename grammar and path-normalization scoping.
#endregion

// ReSharper disable InconsistentNaming
namespace FeatureFilenameGrammarAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;

public class Should_Enforce_Feature_Filename_Grammar
{
  private const string MinimalSource =
    """
    #region Purpose
    // Test fixture for filename-grammar analyzer path tests.
    #endregion

    namespace Test;

    public static class Marker;
    """;

  private static CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> Test(
    string filePath,
    string source = MinimalSource) =>
    new()
    {
      TestState =
      {
        Sources =
        {
          (filePath, source)
        }
      }
    };

  // First non-trivia token of MinimalSource is `namespace` on line 5 (cols 1–10).
  private static DiagnosticResult Twa0015(string path, string fileName, string function, string requiredLayer) =>
    new DiagnosticResult(id: FeatureFilenameGrammarAnalyzer.PairingMismatchId, DiagnosticSeverity.Warning)
      .WithSpan(path, 5, 1, 5, 10)
      .WithArguments
      (
        fileName,
        function,
        requiredLayer,
        "-endpoint- ⇒ -server, -feature-annotations- ⇒ -server, -handler- ⇒ -application"
      );

  private static DiagnosticResult Twa0016(string path, string fileName, string function) =>
    new DiagnosticResult(id: FeatureFilenameGrammarAnalyzer.UnregisteredFunctionId, DiagnosticSeverity.Warning)
      .WithSpan(path, 5, 1, 5, 10)
      .WithArguments
      (
        fileName,
        function,
        "-endpoint-, -feature-annotations-, -handler-"
      );

  public static async Task Given_Handler_On_Application_IsClean()
  {
    await Test("../features/hello/hello-handler-application.cs").RunAsync();
  }

  public static async Task Given_Handler_On_Contracts_Flags_TWA0015_ProjectRelativePath()
  {
    const string Path = "../features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(Path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Handler_On_Contracts_Flags_TWA0015_CollapsedTraversalPath()
  {
    // Spike pitfall: FilePath often arrives as project-relative WITH `..` traversal through
    // the layer project directory (e.g. web-server/../features/...). Must still flag.
    const string Path = "web-server/../features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(Path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_FeatureAnnotations_On_Server_IsClean()
  {
    await Test("../features/hello/hello-feature-annotations-server.cs").RunAsync();
  }

  public static async Task Given_FeatureAnnotations_On_Application_Flags_TWA0015()
  {
    const string Path = "../features/hello/hello-feature-annotations-application.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add
    (
      Twa0015(Path, "hello-feature-annotations-application.cs", "feature-annotations", "server")
    );
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Contracts_Escape_Form_IsClean()
  {
    await Test("../features/hello/hello-contracts.cs").RunAsync();
  }

  public static async Task Given_MultiHyphen_Escape_Hatch_IsClean()
  {
    // role-store / web-authn-* — no registered function; entire pre-layer is the name.
    await Test("../features/admin/roles/role-store-application.cs").RunAsync();
    await Test("../features/identity/web-authn-payload-decoder-application.cs").RunAsync();
  }

  public static async Task Given_Wrong_Case_Function_Flags_TWA0016()
  {
    const string Path = "../features/hello/hello-Handler-application.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0016(Path, "hello-Handler-application.cs", "Handler"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Incomplete_MultiSegment_Function_Flags_TWA0016()
  {
    // feature-annotations is registered; a feature-*-annotations form that is not the full
    // registered function is an incomplete archetype (TWA0016), not an escape-hatch name.
    const string Path = "../features/hello/feature-only-annotations-server.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add
    (
      Twa0016(Path, "feature-only-annotations-server.cs", "feature-only-annotations")
    );
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Layer_Project_Features_Path_IsSilent()
  {
    // Must not flag files still under a layer project's own features/ folder, and must not
    // be fooled by a bare "web-server/" substring in an unrelated path.
    await Test("web-application/features/hello/hello-handler.cs").RunAsync();
    await Test("/repo/source/container-apps/web/web-server/features/hello/feature-annotations.cs")
      .RunAsync();
  }

  public static async Task Given_Outside_Features_Tree_IsSilent()
  {
    await Test("services/cookie-browser-session-service.cs").RunAsync();
    await Test("web-server/program.cs").RunAsync();
  }

  public static async Task Given_Absolute_Cohesive_Path_Flags_TWA0015()
  {
    const string Path =
      "/home/dev/source/container-apps/web/features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(Path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }
}

public class Should_Keep_Grammar_Registry_In_Sync
{
  public static void Json_Cs_And_Props_Have_No_Drift()
  {
    string repoRoot = FindRepoRoot();
    string jsonPath = Path.Combine
    (
      repoRoot,
      "source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json"
    );
    string csPath = Path.Combine
    (
      repoRoot,
      "source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.g.cs"
    );
    string propsPath = Path.Combine
    (
      repoRoot,
      "source/container-apps/web/msbuild/feature-filename-grammar.g.props"
    );

    File.Exists(jsonPath).ShouldBeTrue($"Missing {jsonPath}");
    File.Exists(csPath).ShouldBeTrue($"Missing {csPath}");
    File.Exists(propsPath).ShouldBeTrue($"Missing {propsPath}");

    using System.Text.Json.JsonDocument document =
      System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    System.Text.Json.JsonElement root = document.RootElement;

    string[] layers = root.GetProperty("layers").EnumerateArray()
      .Select(static e => e.GetString()!)
      .ToArray();
    Dictionary<string, string> functions = root.GetProperty("functions").EnumerateObject()
      .ToDictionary(static p => p.Name, static p => p.Value.GetString()!);

    // C# constants must agree with the live FeatureFilenameGrammar type (compiled from .g.cs).
    FeatureFilenameGrammar.Layers.OrderBy(static l => l).ShouldBe(layers.OrderBy(static l => l));
    FeatureFilenameGrammar.FunctionToLayer.Count.ShouldBe(functions.Count);
    foreach (KeyValuePair<string, string> pair in functions)
    {
      FeatureFilenameGrammar.FunctionToLayer.ContainsKey(pair.Key).ShouldBeTrue();
      FeatureFilenameGrammar.FunctionToLayer[pair.Key].ShouldBe(pair.Value);
    }

    // Props items must list the same layers and function→layer pairs.
    string props = File.ReadAllText(propsPath);
    foreach (string layer in layers)
    {
      props.ShouldContain($"FeatureFilenameGrammarLayer Include=\"{layer}\"");
    }

    foreach (KeyValuePair<string, string> pair in functions)
    {
      props.ShouldContain
      (
        $"FeatureFilenameGrammarFunction Include=\"{pair.Key}\" Layer=\"{pair.Value}\""
      );
    }
  }

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source/Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}
