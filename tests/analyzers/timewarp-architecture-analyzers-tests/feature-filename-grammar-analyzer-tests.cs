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
        "-endpoint- ⇒ -server, -handler- ⇒ -application"
      );

  private static DiagnosticResult Twa0016(string path, string fileName, string function) =>
    new DiagnosticResult(id: FeatureFilenameGrammarAnalyzer.UnregisteredFunctionId, DiagnosticSeverity.Warning)
      .WithSpan(path, 5, 1, 5, 10)
      .WithArguments
      (
        fileName,
        function,
        "-endpoint-, -handler-"
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

  public static async Task Given_Endpoint_On_Server_IsClean()
  {
    await Test("../features/hello/hello-endpoint-server.cs").RunAsync();
  }

  public static async Task Given_Endpoint_On_Application_Flags_TWA0015()
  {
    const string Path = "../features/hello/hello-endpoint-application.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, FixieVerifier> analyzerTest = Test(Path);
    analyzerTest.ExpectedDiagnostics.Add
    (
      Twa0015(Path, "hello-endpoint-application.cs", "endpoint", "server")
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

  public static async Task Given_Layer_Project_Features_Path_IsSilent()
  {
    // Must not flag files still under a layer project's own features/ folder, and must not
    // be fooled by a bare "web-server/" substring in an unrelated path.
    await Test("web-application/features/hello/hello-handler.cs").RunAsync();
    await Test("/repo/source/container-apps/web/projects/web-server/features/hello/some-helper.cs")
      .RunAsync();
  }

  public static async Task Given_Spa_Features_Paths_AreSilent_Even_With_Grammar_Names()
  {
    // SPA stays conventional (axis-1). Project-relative features/… and absolute web-spa/features/…
    // must never enter the cohesive-tree scope, even if the filename looks grammar-shaped.
    await Test("features/counter/counter-handler-application.cs").RunAsync();
    await Test("/repo/source/container-apps/web/projects/web-spa/features/counter/counter-handler-application.cs")
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
  // Family list must stay in sync with the convention-analyzers csproj's three <Exec> invocations
  // (source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj).
  // yarp is a single-project family (no concern trees) and is intentionally excluded (127 precedent).
  private static readonly (string Prefix, string Family)[] Families =
  [
    ("Web", "web"),
    ("Api", "api"),
    ("Grpc", "grpc"),
  ];

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

    File.Exists(jsonPath).ShouldBeTrue($"Missing {jsonPath}");
    File.Exists(csPath).ShouldBeTrue($"Missing {csPath}");

    using System.Text.Json.JsonDocument document =
      System.Text.Json.JsonDocument.Parse(File.ReadAllText(jsonPath));
    System.Text.Json.JsonElement root = document.RootElement;

    string[] layers = root.GetProperty("layers").EnumerateArray()
      .Select(static e => e.GetString()!)
      .ToArray();
    Dictionary<string, string> functions = root.GetProperty("functions").EnumerateObject()
      .ToDictionary(static p => p.Name, static p => p.Value.GetString()!);

    // C# constants must agree with the live FeatureFilenameGrammar type (compiled from .g.cs).
    // The registry itself is family-agnostic (Decision 2, task 129 stage 0) — checked once.
    FeatureFilenameGrammar.Layers.OrderBy(static l => l).ShouldBe(layers.OrderBy(static l => l));
    FeatureFilenameGrammar.FunctionToLayer.Count.ShouldBe(functions.Count);
    foreach (KeyValuePair<string, string> pair in functions)
    {
      FeatureFilenameGrammar.FunctionToLayer.ContainsKey(pair.Key).ShouldBeTrue();
      FeatureFilenameGrammar.FunctionToLayer[pair.Key].ShouldBe(pair.Value);
    }

    // Each family gets its own generated props + membership targets, from the same JSON.
    foreach ((string prefix, string family) in Families)
    {
      string propsPath = Path.Combine
      (
        repoRoot,
        $"source/container-apps/{family}/msbuild/feature-filename-grammar.g.props"
      );
      File.Exists(propsPath).ShouldBeTrue($"Missing {propsPath}");

      // Props items must list the same layers and function→layer pairs, plus generated hybrid
      // globs for both cohesive trees (features/ + platform/).
      string props = File.ReadAllText(propsPath);
      foreach (string layer in layers)
      {
        props.ShouldContain($"FeatureFilenameGrammarLayer Include=\"{layer}\"");
        props.ShouldContain($"$({prefix}FeatureTreeRoot)/**/*-{layer}.cs");
        props.ShouldContain($"$({prefix}PlatformTreeRoot)/**/*-{layer}.cs");
        props.ShouldContain($"'$(MSBuildProjectName)' == '{family}-{layer}'");
      }

      props.ShouldContain("FeatureFilenameLayerSuffixRegex");
      props.ShouldContain($"{prefix}PlatformTreeRoot");
      props.ShouldContain("Link=\"platform\\%(RecursiveDir)%(Filename)%(Extension)\"");

      foreach (KeyValuePair<string, string> pair in functions)
      {
        props.ShouldContain
        (
          $"FeatureFilenameGrammarFunction Include=\"{pair.Key}\" Layer=\"{pair.Value}\""
        );
      }

      // Membership targets must not re-hand-list layer globs; they consume the generated props
      // and define both tree roots + membership scan both.
      string membershipPath = Path.Combine
      (
        repoRoot,
        $"source/container-apps/{family}/msbuild/feature-membership.targets"
      );
      File.Exists(membershipPath).ShouldBeTrue($"Missing {membershipPath}");
      string membership = File.ReadAllText(membershipPath);
      membership.ShouldContain("feature-filename-grammar.g.props");
      membership.ShouldContain("FeatureFilenameLayerSuffixRegex");
      membership.ShouldContain($"{prefix}FeatureTreeRoot");
      membership.ShouldContain($"{prefix}PlatformTreeRoot");
      membership.ShouldContain($"$({prefix}PlatformTreeRoot)/**/*.cs");
      membership.ShouldNotContain("**/*-contracts.cs");
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
