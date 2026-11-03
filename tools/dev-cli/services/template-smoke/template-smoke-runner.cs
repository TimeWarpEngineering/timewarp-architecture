#region Purpose
// Orchestrates dev template-smoke: pack, install, per-variant generate/build/test tiers, ingress-port check.
#endregion
#region Design
// Smoke-only orchestration: platform namespace-literal scan, pack platform packages + template at
// 2.0.0-smoke, local feed NuGet.config, CPM pin rewrite, and package-mode checks (no vendored trees /
// removed symbols). Shared rewrite asserts and the generate/restore/build skeleton live in
// the types TemplateSmokeHarness composes (task 131-003 / F-007).
// Task 135: SmokeOneAsync runs co-located-Jaribu tiers per matrix entry - tier 1
// (AssertCoLocatedTestFilesSurviveGeneration, inside afterGenerateAsync, before restore/build) and
// tier 2 (AssertCoLocatedTestFilesRunAsync, after the solution build succeeds). Both are necessary:
// the ordinary solution build never compiles these files (they're a registered-unrouted "tests"
// grammar suffix - feature-filename-grammar.json - that claims no layer project's Compile glob by
// design), so without this pair a regression to the JARIBU_MULTI cnd:noEmit escape would ship silently.
// Task 136: tier 3 (AssertJaribuFamilyAggregatorsAsync) after tier 2 - bare `dotnet test -c Release`
// from each generated family aggregator dir; the count discovered per family tracks however many
// co-located Jaribu runfiles exist there at generation time (do not hardcode a tally here - it drifts
// as tests are added). Aggregators are also not in .slnx, so solution build is blind to multi-mode
// compile + MTP discovery.
// Task 240: after the generated solution build, AssertInitializerImportGraphResolves walks
// web.spa.lib.module.js static `_content` imports against the NuGet cache (no server).
// Task 288: every matrix entry asserts the AppHost's committed deploy parameters carry the app's
// kebab name, and a generate-only Contoso.Shop proves the dotted-name -> contoso-shop derivation
// without paying for another restore/build.
// Task 373: a web-off entry skips AssertMockAuthFailClosedSurfaces.
// Task 375: the shared pack, install and dotted-name asserts run once; the SmokeMatrix loop runs
// SmokeDefault alone unless --matrix selects every entry. Each flag adds a variant and minutes, so
// PR/push CI runs SmokeDefault only and a nightly CI run (schedule, or workflow_dispatch mode nightly)
// runs `--matrix --app-tests`. Local default matches the PR gate; pass --matrix when a change touches
// `#if flag` regions or template.json symbols. --app-tests adds AssertGeneratedAppTestSuitesAsync per
// variant (needs Docker). The loop runs every selected variant even after one fails and reports them
// together, so one nightly names every broken flag.
#endregion

namespace DevCli.Services;

using static DevCli.Services.TemplateSmokeMatrix;

/// <summary>
/// Runs the template-smoke gate end to end for the selected variants.
/// </summary>
internal sealed class TemplateSmokeRunner
{
  private readonly ITerminal Terminal;
  private bool Matrix;
  private bool AppTests;
  private CancellationToken Ct;
  private string RepoRoot = null!;
  private string SmokeRoot = null!;
  private string PackagesDir = null!;
  private string WorkDir = null!;
  private TemplateSmokeHarness Harness = null!;
  private TemplateSmokePackaging Packaging = null!;
  private TemplateSmokeGeneratedAppChecks Checks = null!;
  private readonly List<int> IngressPorts = [];

  public TemplateSmokeRunner(ITerminal terminal)
  {
    Terminal = terminal;
  }

  public async Task RunAsync(bool matrix, bool appTests, CancellationToken ct)
  {
    Matrix = matrix;
    AppTests = appTests;
    Ct = ct;

    if (!FindRepoRoot()) return;

    SmokeRoot = Path.Combine(RepoRoot, "artifacts", "template-smoke");
    PackagesDir = Path.Combine(SmokeRoot, "packages");
    WorkDir = Path.Combine(SmokeRoot, "work");
    Harness = new TemplateSmokeHarness(Terminal, RepoRoot);
    Packaging = new TemplateSmokePackaging(Terminal, RepoRoot, PackagesDir, WorkDir, Harness.Suffixes);
    Checks = new TemplateSmokeGeneratedAppChecks(Terminal, Harness.Pins);

    Terminal.WriteLine($"\nTemplate smoke — work root: {SmokeRoot}\n".Cyan());

    if (!Harness.Pins.AssertNoUnsafePlatformNamespaceLiterals()) return;
    if (!Packaging.AssertRemovedPackageSymbolsGoneFromTemplateConfig()) return;
    if (!await Packaging.PackPlatformPackagesAsync(Ct)) return;
    if (!await Packaging.PackTemplateAsync(Ct)) return;
    if (!await Packaging.InstallTemplateAsync(Ct)) return;
    if (!await AssertDottedNameKebabsAsync()) return;

    (string Name, string[] ExtraArgs, string[] ExcludedFamilies)[] variants = Matrix
      ? SmokeMatrix
      : [.. SmokeMatrix.Where(variant => variant.ExtraArgs.Length == 0)];
    Terminal.WriteLine(
      $"Variants: {string.Join(", ", variants.Select(v => v.Name))}" +
      (Matrix ? "" : " (--matrix adds each flag off)") +
      (AppTests ? "; generated-app test projects included" : ""));

    List<string> failedVariants = [];
    foreach ((string name, string[] extraArgs, string[] excludedFamilies) in variants)
    {
      if (!await SmokeOneAsync(name, extraArgs, excludedFamilies))
      {
        Terminal.WriteErrorLine($"\nTemplate smoke FAILED — {name}".Red());
        failedVariants.Add(name);
      }
    }

    if (failedVariants.Count > 0)
    {
      Terminal.WriteErrorLine($"\nTemplate smoke FAILED — {string.Join(", ", failedVariants)}".Red());
      Environment.ExitCode = 1;
      return;
    }

    if (!AssertIngressPortsRegenerated()) return;

    Terminal.WriteLine("\nTemplate smoke SUCCEEDED".Green());
  }

  private bool FindRepoRoot()
  {
    string? root = Git.FindRoot();
    if (root is null)
    {
      Terminal.WriteErrorLine("Error: could not find repository root.");
      Environment.ExitCode = 1;
      return false;
    }

    RepoRoot = root;
    return true;
  }

  private async Task<bool> AssertDottedNameKebabsAsync()
  {
    string? outputDir = await Harness.Generation.GenerateAsync(DottedSmokeName, WorkDir, [], TemplateShortName, Ct);
    if (outputDir is not null
      && Harness.Content.AssertDeployParametersUseAppKebabName(outputDir, DottedSmokeKebabName)
      && RecordIngressPort(outputDir))
    {
      return true;
    }

    Terminal.WriteErrorLine($"\nTemplate smoke FAILED — {DottedSmokeName}".Red());
    Environment.ExitCode = 1;
    return false;
  }

  private bool RecordIngressPort(string outputDir)
  {
    if (Harness.Content.ReadGeneratedIngressPort(outputDir) is not { } port)
      return false;

    IngressPorts.Add(port);
    return true;
  }

  /// <summary>
  /// Task 311: each generation draws its own ingress-port. The generator is random, so two apps may collide by chance;
  /// every generation of the run sharing one value means the port was not regenerated.
  /// </summary>
  private bool AssertIngressPortsRegenerated()
  {
    if (IngressPorts.Count < 2)
    {
      Terminal.WriteLine($"Generated ingress-ports: {string.Join(", ", IngressPorts)} (fewer than two generations; per-app difference not checked).");
      return true;
    }

    if (IngressPorts.Distinct().Count() > 1)
    {
      Terminal.WriteLine($"Generated ingress-ports differ per app: {string.Join(", ", IngressPorts)}.");
      return true;
    }

    Terminal.WriteErrorLine($"\nTemplate smoke FAILED — all {IngressPorts.Count} generated apps share ingress-port {IngressPorts[0]}".Red());
    Environment.ExitCode = 1;
    return false;
  }

  private async Task<bool> SmokeOneAsync(string name, string[] extraArgs, string[] excludedFamilies)
  {
    string? outputDirForTier2 = null;

    bool buildOk = await Harness.Generation.SmokeOneAsync(
      name,
      WorkDir,
      extraArgs,
      TemplateShortName,
      Ct,
      afterGenerateAsync: outputDir =>
      {
        outputDirForTier2 = outputDir;

        if (!Checks.AssertGeneratedAppPackageMode(name, outputDir))
          return Task.FromResult(false);

        if (!Harness.Content.AssertSkillsShipped(outputDir))
          return Task.FromResult(false);

        if (!Harness.Content.AssertKanbanSkeletonShipped(outputDir))
          return Task.FromResult(false);

        // Task 288: matrix names carry no separators, so the kebab name is the lower-cased name.
        if (!Harness.Content.AssertDeployParametersUseAppKebabName(outputDir, name.ToLowerInvariant()))
          return Task.FromResult(false);

        // Task 311: the ingressPort port generator gave this app its own ingress-port.
        if (!RecordIngressPort(outputDir))
          return Task.FromResult(false);

        // Task 145-009: Production appsettings must not enable mock auth; mock registration
        // type must exist so fail-closed is a product surface, not a missing feature.
        if (!excludedFamilies.Contains("web") && !Checks.AssertMockAuthFailClosedSurfaces(name, outputDir))
          return Task.FromResult(false);

        // Tier 1 (cheap): the JARIBU_MULTI guard must survive dotnet-new generation verbatim —
        // check right after generate, before spending time on restore/build (task 135).
        if (!Harness.TestRuns.AssertCoLocatedTestFilesSurviveGeneration(outputDir, excludedFamilies))
          return Task.FromResult(false);

        if (!Harness.Pins.RewriteCpmPins(outputDir, SmokePackageVersion))
          return Task.FromResult(false);

        Packaging.WriteLocalNuGetConfig(outputDir);
        return Task.FromResult(true);
      },
      restoreNarration: " (local TimeWarp.* feed)");

    if (!buildOk)
      return false;

    if (outputDirForTier2 is null)
      return false;

    // Task 240: generated SPA initializer `_content` specifiers must exist in the NuGet cache
    // (State.Plus kebab-case rename). Host-free; the HTTP graph smoke lives in web-server tests.
    if (!Harness.InitializerAssets.AssertInitializerImportGraphResolves(outputDirForTier2))
      return false;

    // Tier 2 (authoritative): `dotnet run` each generated co-located test standalone and
    // confirm it actually passes end to end post-generation (task 135). Runs after the solution
    // build succeeds — the co-located files compile into no layer project, so the solution
    // build itself is structurally blind to them; this is the only step that runs them standalone.

    if (!await Harness.TestRuns.AssertCoLocatedTestFilesRunAsync(outputDirForTier2, excludedFamilies, Ct))
      return false;

    // Tier 3 (task 136): family JARIBU_MULTI aggregators — multi-mode compile + MTP bare
    // `dotnet test` from each project dir. Not in .slnx. TIMEWARP_TEST_PORT_BASE isolates
    // generated-app hosts from monorepo `dev test` (task 245). Flag-off entries assert
    // excluded families' aggregators are absent (review R2-1).
    if (!await Harness.TestRuns.AssertJaribuFamilyAggregatorsAsync(outputDirForTier2, excludedFamilies, Ct))
      return false;

    // Task 375: opt-in generated-app test projects (nightly CI).
    return !AppTests || await Harness.TestRuns.AssertGeneratedAppTestSuitesAsync(outputDirForTier2, Ct);
  }
}
