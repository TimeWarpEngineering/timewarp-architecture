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
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Enforce_Feature_Filename_Grammar>();

  private const string MinimalSource =
    """
    #region Purpose
    // Test fixture for filename-grammar analyzer path tests.
    #endregion

    namespace Test;

    public static class Marker;
    """;

  private static CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> Test(
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
    const string path = "../features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Handler_On_Contracts_Flags_TWA0015_CollapsedTraversalPath()
  {
    // Spike pitfall: FilePath often arrives as project-relative WITH `..` traversal through
    // the layer project directory (e.g. web-server/../features/...). Must still flag.
    const string path = "web-server/../features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Endpoint_On_Server_IsClean()
  {
    await Test("../features/hello/hello-endpoint-server.cs").RunAsync();
  }

  public static async Task Given_Endpoint_On_Application_Flags_TWA0015()
  {
    const string path = "../features/hello/hello-endpoint-application.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add
    (
      Twa0015(path, "hello-endpoint-application.cs", "endpoint", "server")
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
    const string path = "../features/hello/hello-Handler-application.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0016(path, "hello-Handler-application.cs", "Handler"));
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

  public static async Task Given_Api_Family_Layer_Project_Features_Path_IsSilent()
  {
    // Family-generic markers (task 129): api-* layer projects' own features/ folders are not
    // the api family's cohesive tree either.
    await Test("api-application/features/weather-forecast/weather-forecast-handler.cs").RunAsync();
    await Test("/repo/source/container-apps/api/projects/api-server/features/weather-forecast/some-helper.cs")
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
    const string path =
      "/home/dev/source/container-apps/web/features/hello/hello-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(path, "hello-handler-contracts.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Absolute_Api_Family_Cohesive_Path_Flags_TWA0015()
  {
    // Family-generic scoping (task 129): api/features/ is a cohesive tree too, not just web's.
    const string path =
      "/home/dev/source/container-apps/api/features/weather-forecast/weather-forecast-handler-contracts.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add
    (
      Twa0015(path, "weather-forecast-handler-contracts.cs", "handler", "application")
    );
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Api_Family_Relative_Cohesive_Path_IsClean()
  {
    // ../features/ glob form is family-agnostic already; confirm api-shaped names parse clean.
    await Test("../features/weather-forecast/get-weather-forecasts/get-weather-forecasts-contracts.cs")
      .RunAsync();
  }

  // "tests" is a registered-unrouted layer (task 135): matched and validated exactly like a
  // routed layer, but claims no layer project's Compile glob. Registering it ONLY as a layer
  // (nothing added to "functions") means TWA0015 fires on `-handler-tests.cs`/`-endpoint-tests.cs`
  // for free through the existing pairing logic — zero analyzer code changes.
  public static async Task Given_Tests_Escape_Form_IsClean()
  {
    await Test("../features/hello/hello-tests.cs").RunAsync();
    await Test("../features/admin/roles/create-role/create-role-tests.cs").RunAsync();
  }

  public static async Task Given_Handler_On_Tests_Flags_TWA0015()
  {
    const string path = "../features/hello/hello-handler-tests.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(path, "hello-handler-tests.cs", "handler", "application"));
    await analyzerTest.RunAsync();
  }

  public static async Task Given_Endpoint_On_Tests_Flags_TWA0015()
  {
    const string path = "../features/hello/hello-endpoint-tests.cs";
    CSharpAnalyzerTest<FeatureFilenameGrammarAnalyzer, RoslynTestVerifier> analyzerTest = Test(path);
    analyzerTest.ExpectedDiagnostics.Add(Twa0015(path, "hello-endpoint-tests.cs", "endpoint", "server"));
    await analyzerTest.RunAsync();
  }
}

public class Should_Keep_Grammar_Registry_In_Sync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Keep_Grammar_Registry_In_Sync>();

  // Family list must stay in sync with the convention-analyzers csproj's three <Exec> invocations
  // (source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj).
  // yarp is a single-project family (no concern trees) and is intentionally excluded (127 precedent).
  private static readonly (string Prefix, string Family)[] Families =
  [
    ("Web", "web"),
    ("Api", "api"),
    ("Grpc", "grpc"),
  ];

  public static Task Json_Cs_And_Props_Have_No_Drift()
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
    // Registered-unrouted layers (task 135, e.g. "tests"): matched/validated exactly like a
    // routed layer, but own no layer-project Compile glob. Optional property — absent JSON stays
    // "no unrouted layers registered".
    string[] unroutedLayers = root.TryGetProperty("unroutedLayers", out System.Text.Json.JsonElement unroutedElement)
      ? unroutedElement.EnumerateArray().Select(static e => e.GetString()!).ToArray()
      : [];
    Dictionary<string, string> functions = root.GetProperty("functions").EnumerateObject()
      .ToDictionary(static p => p.Name, static p => p.Value.GetString()!);

    // C# constants must agree with the live FeatureFilenameGrammar type (compiled from .g.cs).
    // The registry itself is family-agnostic (Decision 2, task 129 stage 0) — checked once.
    // FeatureFilenameGrammar.Layers is routed ∪ unrouted (the analyzer accepts both as valid
    // archetypes; only routed layers get a layer-project Compile glob — checked below).
    FeatureFilenameGrammar.Layers.OrderBy(static l => l)
      .ShouldBe(layers.Concat(unroutedLayers).Distinct().OrderBy(static l => l));
    FeatureFilenameGrammar.FunctionToLayer.Count.ShouldBe(functions.Count);
    foreach (KeyValuePair<string, string> pair in functions)
    {
      FeatureFilenameGrammar.FunctionToLayer.ContainsKey(pair.Key).ShouldBeTrue();
      FeatureFilenameGrammar.FunctionToLayer[pair.Key].ShouldBe(pair.Value);

      // Functions register ONLY against routed layers (task 135 decision — "tests" is a
      // layer-only registration, nothing added to "functions"). A function pointed at an
      // unrouted layer would be nonsensical (an archetype whose "correct" layer compiles
      // nowhere), so guard against that drift too.
      unroutedLayers.ShouldNotContain(pair.Value);
    }

    // The .g.cs Families constant (sourced from the csproj's Web <Exec> --families argument)
    // must agree with this test's own family list — the documented duplication (task 129
    // stage 1) is now a checked one rather than a silent hand-sync.
    FeatureFilenameGrammar.Families.OrderBy(static f => f, StringComparer.Ordinal)
      .ShouldBe(Families.Select(static f => f.Family).OrderBy(static f => f, StringComparer.Ordinal));

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
        props.ShouldContain($"FeatureFilenameGrammarLayer Include=\"{layer}\" Project=\"{family}-{layer}\"");
        props.ShouldContain($"$({prefix}FeatureTreeRoot)/**/*-{layer}.cs");
        props.ShouldContain($"$({prefix}PlatformTreeRoot)/**/*-{layer}.cs");
        props.ShouldContain($"'$(MSBuildProjectName)' == '{family}-{layer}'");
      }

      // Registered-unrouted layers get a layer item with NO Project metadata, and — the whole
      // point — NO Compile ItemGroup at all (they must not claim a layer project's build).
      foreach (string layer in unroutedLayers)
      {
        props.ShouldContain($"FeatureFilenameGrammarLayer Include=\"{layer}\" />");
        props.ShouldNotContain($"FeatureFilenameGrammarLayer Include=\"{layer}\" Project=");
        props.ShouldNotContain($"$({prefix}FeatureTreeRoot)/**/*-{layer}.cs");
        props.ShouldNotContain($"$({prefix}PlatformTreeRoot)/**/*-{layer}.cs");
        props.ShouldNotContain($"'$(MSBuildProjectName)' == '{family}-{layer}'");
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

    return Task.CompletedTask;
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
