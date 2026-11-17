#region Purpose
// Package-mode and mock-auth asserts on a freshly generated smoke app.
#endregion
#region Design
// Post-generate: no vendored platform trees; removed *Packages symbols gone; belt-and-suspenders scan
// for rewritten {appName}.(Analyzers|Generators|Attributes|TypedIds) including .cs. Suffixes come from
// TemplateSmokeArchitectureSuffixes (props SSOT derivation).
// Task 145-009 / 373: Production cannot activate mock auth via config alone, and the fail-closed
// registration helper must still exist in the generated SPA tree. The web family's mock-auth surfaces
// exist only with web on, so the runner skips the mock-auth assert for a web-off entry.
#endregion

namespace DevCli.Services;

using static DevCli.Services.TemplateSmokeMatrix;

/// <summary>
/// Template-smoke asserts that read a generated app's files after generation.
/// </summary>
internal sealed partial class TemplateSmokeGeneratedAppChecks
{
  private readonly ITerminal Terminal;
  private readonly TemplateSmokePlatformPins Pins;

  public TemplateSmokeGeneratedAppChecks(ITerminal terminal, TemplateSmokePlatformPins pins)
  {
    Terminal = terminal;
    Pins = pins;
  }

  [GeneratedRegex(@"""UseMock""\s*:\s*true", RegexOptions.IgnoreCase)]
  private static partial Regex UseMockTrueInJson();

  [GeneratedRegex(@"DefineConstants[^>]*MOCK_AUTHENTICATION", RegexOptions.IgnoreCase)]
  private static partial Regex MockAuthenticationInDefineConstants();

  private static readonly string[] VendoredPlatformRelativeTrees =
  [
    "source/foundation",
    "source/libraries/timewarp-modules",
    "source/libraries/timewarp-identity",
    "source/libraries/timewarp-402",
    "source/analyzers",
    "tests/foundation",
    "tests/libraries/timewarp-identity-tests",
    "tests/libraries/timewarp-402-tests",
    "tests/analyzers",
  ];

  /// <summary>
  /// Post-generate: no vendored platform trees; removed *Packages symbols gone; belt-and-suspenders
  /// scan for rewritten {appName}.(Analyzers|Generators|Attributes|TypedIds) including .cs.
  /// Suffixes come from TemplateSmokeArchitectureSuffixes (props SSOT derivation).
  /// </summary>
  public bool AssertGeneratedAppPackageMode(string appName, string outputDir)
  {
    List<string> failures = [];

    foreach (string relativeTree in VendoredPlatformRelativeTrees)
    {
      string path = Path.Combine(outputDir, relativeTree.Replace('/', Path.DirectorySeparatorChar));
      if (Directory.Exists(path))
        failures.Add($"vendored platform tree present: {relativeTree}");
    }

    string generatedTemplateJson = Path.Combine(outputDir, ".template.config", "template.json");
    if (File.Exists(generatedTemplateJson))
    {
      string templateText = File.ReadAllText(generatedTemplateJson);
      foreach (string symbol in RemovedTemplateSymbols)
      {
        if (templateText.Contains(symbol, StringComparison.Ordinal))
          failures.Add($".template.config/template.json still declares symbol '{symbol}'");
      }
    }

    failures.AddRange(Pins.CollectRewrittenPlatformTokenHits(appName, outputDir));

    if (failures.Count > 0)
    {
      Terminal.WriteErrorLine("Generated app is not clean package-mode:".Red());
      foreach (string failure in failures)
        Terminal.WriteErrorLine($"  {failure}");
      return false;
    }

    Terminal.WriteLine("Generated-app package-mode check passed (no vendored trees / removed symbols / rewritten namespaces).");
    return true;
  }

  /// <summary>
  /// Task 145-009: Production cannot activate mock auth via config alone — Production
  /// appsettings must not set Authentication:UseMock true, and the fail-closed registration
  /// helper must still exist in the generated SPA tree.
  /// </summary>
  public bool AssertMockAuthFailClosedSurfaces(string appName, string outputDir)
  {
    List<string> failures = [];

    string productionAppsettings = Path.Combine
    (
      outputDir,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-server",
      "appsettings.Production.json"
    );
    if (File.Exists(productionAppsettings))
    {
      string text = File.ReadAllText(productionAppsettings);
      // Coarse but sufficient: Production must not enable the mock flag.
      if (UseMockTrueInJson().IsMatch(text))
      {
        failures.Add("web-server appsettings.Production.json sets Authentication UseMock true");
      }
    }

    string registration = Path.Combine
    (
      outputDir,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "services",
      "mocks",
      "mock-authentication-registration.cs"
    );
    if (!File.Exists(registration))
      failures.Add("generated SPA missing mock-authentication-registration.cs (fail-closed gate surface)");

    string spaCsproj = Path.Combine
    (
      outputDir,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "web-spa.csproj"
    );
    if (File.Exists(spaCsproj))
    {
      string spaText = File.ReadAllText(spaCsproj);
      // Only fail on an active DefineConstants entry — comments may still name the old symbol.
      if (MockAuthenticationInDefineConstants().IsMatch(spaText))
      {
        failures.Add("web-spa.csproj still defines MOCK_AUTHENTICATION (should be runtime-gated only)");
      }
    }

    if (failures.Count > 0)
    {
      Terminal.WriteErrorLine($"{appName}: mock-auth fail-closed smoke failed:".Red());
      foreach (string failure in failures)
        Terminal.WriteErrorLine($"  {failure}");
      return false;
    }

    Terminal.WriteLine($"{appName}: mock-auth fail-closed surfaces OK.");
    return true;
  }
}
