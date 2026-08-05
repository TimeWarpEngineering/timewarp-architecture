#region Purpose
// Pack, install, generate, and build the template to catch sourceName/package dual-mode regressions.
#endregion
#region Design
// Regression gate for task 115: template sourceName is TimeWarp.Architecture, so package-id
// literals TimeWarp.Architecture.* used to rewrite to SmokeDefault.* and break restore. Smoke
// always generates with names ≠ sourceName (SmokeDefault, SmokeNoPostgres, SmokeNoApi) so rewrite bugs
// surface. Matrix: defaults + --postgres false.
//
// Shared rewrite asserts, props SSOT suffix derivation, pin matching, and generate/restore/build
// skeleton live in TemplateSmokeHarness (task 131-003 / F-007). This command owns smoke-only
// orchestration: pack platform packages + template at 2.0.0-smoke, local feed NuGet.config,
// CPM pin rewrite, and package-mode checks (no vendored trees / removed symbols).
//
// Packs the template AND monorepo platform packages (foundation + analyzers/generators/attributes
// + modules + identity) into a local feed. Published nuget.org pins lag monorepo API surface;
// smoke validates "this branch's template + this branch's packages". Work dir:
// artifacts/template-smoke/. Prefer `dotnet run tools/dev-cli/dev.cs -- template-smoke` (or
// re-self-install) over a stale AOT `./bin/dev`; CI always uses the runfile path.
//
// Task 135: SmokeOneAsync runs co-located-Jaribu tiers per matrix entry —
// tier 1 (Harness.AssertCoLocatedTestFilesSurviveGeneration, inside afterGenerateAsync, before
// restore/build) and tier 2 (Harness.AssertCoLocatedTestFilesRunAsync, after the solution build
// succeeds). Both are necessary: the ordinary solution build never compiles these files (they're
// a registered-unrouted "tests" grammar suffix — feature-filename-grammar.json — that claims no
// layer project's Compile glob by design), so without this pair a regression to the JARIBU_MULTI
// cnd:noEmit escape would ship silently.
// Task 136: tier 3 (Harness.AssertJaribuFamilyAggregatorsAsync) after tier 2 — bare
// `dotnet test -c Release` from each generated family aggregator dir; the count discovered per
// family tracks however many co-located Jaribu runfiles exist there at generation time (do not
// hardcode a tally here — it drifts as tests are added). Aggregators are also not in .slnx, so
// solution build is blind to multi-mode compile + MTP discovery.
#endregion

namespace DevCli.Commands;

[NuruRoute("template-smoke", Description = "Pack/install template and smoke-build generated apps (defaults + --postgres false)")]
internal sealed class TemplateSmokeCommand : ICommand<Unit>
{
  private const string TemplateProject =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  private const string TemplatePackageId = "TimeWarp.Architecture";
  private const string TemplateShortName = "timewarp-architecture";

  /// <summary>
  /// Unique version for smoke-local packs so the global NuGet cache cannot shadow them with
  /// same-version nuget.org bits that lag monorepo API surface (Entity, EndpointAllowAnonymous, …).
  /// Generated Directory.Packages.props pins for these PackageIds are rewritten to this version.
  /// </summary>
  private const string SmokePackageVersion = "2.0.0-smoke";

  private static readonly string[] PlatformPackageProjects =
  [
    "source/analyzers/timewarp-architecture-attributes/timewarp-architecture-attributes.csproj",
    "source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj",
    "source/analyzers/timewarp-architecture-analyzers/timewarp-architecture-analyzers.csproj",
    "source/foundation/foundation-domain/foundation-domain.csproj",
    "source/foundation/foundation-contracts/foundation-contracts.csproj",
    "source/foundation/foundation-application/foundation-application.csproj",
    "source/foundation/foundation-infrastructure/foundation-infrastructure.csproj",
    "source/foundation/foundation-server/foundation-server.csproj",
    "source/libraries/timewarp-modules/timewarp-modules.csproj",
    // Identity is always package-mode for generated apps (task 126-004). Without a smoke-local
    // pack, generated apps would pull nuget.org's TimeWarp.Identity whose Foundation dependency
    // floor (>= the published version) collides with the 2.0.0-smoke pins → NU1603 under
    // -warnaserror.
    "source/libraries/timewarp-identity/timewarp-identity.csproj",
    // TimeWarp.402 (task 104-007): same dual-mode exclude as identity; pack when apps reference it.
    "source/libraries/timewarp-402/timewarp-402.csproj",
  ];

  // ExcludedFamilies drives flag-off assertions in the co-located-test tiers: for a family
  // listed here, tiers 1/3 assert its test artifacts are ABSENT from the generated app
  // (task 136 review R2-1 — an orphaned aggregator under a flag-off generation must fail smoke).
  private static readonly (string Name, string[] ExtraArgs, string[] ExcludedFamilies)[] SmokeMatrix =
  [
    ("SmokeDefault", [], []),
    ("SmokeNoPostgres", ["--postgres", "false"], []),
    ("SmokeNoApi", ["--api", "false"], ["api"]),
  ];

  private static readonly string[] RemovedTemplateSymbols =
  [
    "foundationPackages",
    "analyzerPackages",
    "identityPackages",
  ];

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

  internal sealed class Handler : ICommandHandler<TemplateSmokeCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string SmokeRoot = null!;
    private string PackagesDir = null!;
    private string WorkDir = null!;
    private TemplateSmokeHarness Harness = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TemplateSmokeCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;

      SmokeRoot = Path.Combine(RepoRoot, "artifacts", "template-smoke");
      PackagesDir = Path.Combine(SmokeRoot, "packages");
      WorkDir = Path.Combine(SmokeRoot, "work");
      Harness = new TemplateSmokeHarness(Terminal, RepoRoot);

      Terminal.WriteLine($"\nTemplate smoke — work root: {SmokeRoot}\n".Cyan());

      if (!Harness.AssertNoUnsafePlatformNamespaceLiterals()) return Value;
      if (!AssertRemovedPackageSymbolsGoneFromTemplateConfig()) return Value;
      if (!await PackPlatformPackagesAsync()) return Value;
      if (!await PackTemplateAsync()) return Value;
      if (!await InstallTemplateAsync()) return Value;

      foreach ((string name, string[] extraArgs, string[] excludedFamilies) in SmokeMatrix)
      {
        if (!await SmokeOneAsync(name, extraArgs, excludedFamilies))
        {
          Terminal.WriteErrorLine($"\nTemplate smoke FAILED — {name}".Red());
          Environment.ExitCode = 1;
          return Value;
        }
      }

      Terminal.WriteLine("\nTemplate smoke SUCCEEDED".Green());
      return Value;
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

    private async Task<bool> PackPlatformPackagesAsync()
    {
      if (Directory.Exists(PackagesDir))
        Directory.Delete(PackagesDir, recursive: true);
      Directory.CreateDirectory(PackagesDir);

      Terminal.WriteLine($"Packing platform packages @ {SmokePackageVersion} → {PackagesDir}...");
      foreach (string relativeProject in PlatformPackageProjects)
      {
        string project = Path.Combine(RepoRoot, relativeProject);
        Terminal.WriteLine($"  {relativeProject}");
        int exitCode = await DotNet.Pack(project)
          .WithConfiguration("Release")
          .WithOutput(PackagesDir)
          .WithProperty("Version", SmokePackageVersion)
          .WithProperty("PackageVersion", SmokePackageVersion)
          .WithNoValidation()
          .RunAsync(Ct);

        if (exitCode != 0)
        {
          Terminal.WriteErrorLine($"Pack failed for {relativeProject}!".Red());
          Environment.ExitCode = exitCode;
          return false;
        }
      }

      PurgeStaleSmokeCacheEntries();
      return true;
    }

    /// <summary>
    /// The constant 2.0.0-smoke version defends against nuget.org shadowing but NOT against
    /// previous smoke runs: once NuGet's global cache extracts a 2.0.0-smoke package, every
    /// later restore reuses it, so local smoke silently tests stale platform bits (observed:
    /// a week-old TimeWarp.Identity without ListPrincipalsAsync — task 104-035). Evict the
    /// smoke-version cache folder for every id just packed so restore re-extracts this run's
    /// bits. CI is unaffected (cold caches); this makes local runs equally trustworthy.
    /// </summary>
    private void PurgeStaleSmokeCacheEntries()
    {
      string globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

      string suffix = $".{SmokePackageVersion}.nupkg";
      foreach (string nupkg in Directory.GetFiles(PackagesDir, $"*{suffix}"))
      {
        string packageId = Path.GetFileName(nupkg)[..^suffix.Length];
        string cached = Path.Combine(globalPackages, packageId.ToLowerInvariant(), SmokePackageVersion);
        if (Directory.Exists(cached))
        {
          Directory.Delete(cached, recursive: true);
          Terminal.WriteLine($"  evicted stale cache: {packageId}/{SmokePackageVersion}");
        }
      }
    }

    private async Task<bool> PackTemplateAsync()
    {
      if (Directory.Exists(WorkDir))
        Directory.Delete(WorkDir, recursive: true);
      Directory.CreateDirectory(WorkDir);

      string project = Path.Combine(RepoRoot, TemplateProject);
      Terminal.WriteLine($"Packing template → {PackagesDir}...");

      int exitCode = await DotNet.Pack(project)
        .WithConfiguration("Release")
        .WithOutput(PackagesDir)
        .WithNoValidation()
        .RunAsync(Ct);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine("Template pack failed!".Red());
        Environment.ExitCode = exitCode;
        return false;
      }

      return true;
    }

    private async Task<bool> InstallTemplateAsync()
    {
      // Nupkg exclude fragments (e.g. .Analyzers.) derived from props SSOT — not hand lists.
      if (!Harness.TryEnsureArchitectureSuffixes(out _))
        return false;

      string? nupkg = Directory
        .GetFiles(PackagesDir, $"{TemplatePackageId}.*.nupkg")
        .Where(p => !p.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)
                 && !Harness.IsExcludedPlatformNupkgFileName(Path.GetFileName(p)))
        .OrderByDescending(p => p, StringComparer.Ordinal)
        .FirstOrDefault();

      if (nupkg is null)
      {
        Terminal.WriteErrorLine($"No {TemplatePackageId}.*.nupkg found in {PackagesDir}.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine($"Installing template from {nupkg}...");
      // Force reinstall so an older conflicting identity does not win.
      CommandOutput uninstall = await Shell.Builder("dotnet")
        .WithArguments("new", "uninstall", TemplatePackageId)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);
      _ = uninstall; // may fail if not installed

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("new", "install", nupkg)
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!result.Success)
      {
        Terminal.WriteErrorLine(result.Combined);
        Terminal.WriteErrorLine("dotnet new install failed!".Red());
        Environment.ExitCode = result.ExitCode == 0 ? 1 : result.ExitCode;
        return false;
      }

      Terminal.WriteLine(result.Combined);
      return true;
    }

    private async Task<bool> SmokeOneAsync(string name, string[] extraArgs, string[] excludedFamilies)
    {
      string? outputDirForTier2 = null;

      bool buildOk = await Harness.SmokeOneAsync(
        name,
        WorkDir,
        extraArgs,
        TemplateShortName,
        Ct,
        afterGenerateAsync: outputDir =>
        {
          outputDirForTier2 = outputDir;

          if (!AssertGeneratedAppPackageMode(name, outputDir))
            return Task.FromResult(false);

          // Task 145-009: Production appsettings must not enable mock auth; mock registration
          // type must exist so fail-closed is a product surface, not a missing feature.
          if (!AssertMockAuthFailClosedSurfaces(name, outputDir))
            return Task.FromResult(false);

          // Tier 1 (cheap): the JARIBU_MULTI guard must survive dotnet-new generation verbatim —
          // check right after generate, before spending time on restore/build (task 135).
          if (!Harness.AssertCoLocatedTestFilesSurviveGeneration(outputDir, excludedFamilies))
            return Task.FromResult(false);

          if (!Harness.RewriteCpmPins(outputDir, SmokePackageVersion))
            return Task.FromResult(false);

          WriteLocalNuGetConfig(outputDir);
          return Task.FromResult(true);
        },
        restoreNarration: " (local TimeWarp.* feed)");

      if (!buildOk)
        return false;

      // Tier 2 (authoritative): `dotnet run` each generated co-located test standalone and
      // confirm it actually passes end to end post-generation (task 135). Runs after the solution
      // build succeeds — the co-located files compile into no layer project, so the solution
      // build itself is structurally blind to them; this is the only step that runs them standalone.
      if (outputDirForTier2 is null)
        return false;

      if (!await Harness.AssertCoLocatedTestFilesRunAsync(outputDirForTier2, excludedFamilies, Ct))
        return false;

      // Tier 3 (task 136): family JARIBU_MULTI aggregators — multi-mode compile + MTP bare
      // `dotnet test` from each project dir. Serial (api fixed port 7255). Not in .slnx.
      // Flag-off entries assert excluded families' aggregators are absent (review R2-1).
      return await Harness.AssertJaribuFamilyAggregatorsAsync(outputDirForTier2, excludedFamilies, Ct);
    }

    private void WriteLocalNuGetConfig(string outputDir)
    {
      // Absolute path so restore from any working directory resolves the feed.
      string feedPath = PackagesDir;
      string configPath = Path.Combine(outputDir, "NuGet.config");
      string xml =
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="smoke-local" value="{feedPath}" />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
          </packageSources>
          <packageSourceMapping>
            <packageSource key="smoke-local">
              <package pattern="TimeWarp.Architecture" />
              <package pattern="TimeWarp.Architecture.*" />
              <package pattern="TimeWarp.Foundation.*" />
              <package pattern="TimeWarp.Modules" />
              <package pattern="TimeWarp.Identity" />
              <package pattern="TimeWarp.402" />
            </packageSource>
            <packageSource key="nuget.org">
              <package pattern="*" />
            </packageSource>
          </packageSourceMapping>

        </configuration>
        """;
      File.WriteAllText(configPath, xml);
      Terminal.WriteLine($"Wrote {configPath} (TimeWarp.* → smoke-local).");
    }

    /// <summary>
    /// Monorepo template.json must not declare the removed source-mode package symbols
    /// (generated apps never receive .template.config, so this is the authoritative check).
    /// </summary>
    private bool AssertRemovedPackageSymbolsGoneFromTemplateConfig()
    {
      string templateJson = Path.Combine(RepoRoot, ".template.config", "template.json");
      if (!File.Exists(templateJson))
      {
        Terminal.WriteErrorLine($"Missing {templateJson}.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      string text = File.ReadAllText(templateJson);
      var hits = RemovedTemplateSymbols
        .Where(symbol => text.Contains(symbol, StringComparison.Ordinal))
        .ToList();

      if (hits.Count > 0)
      {
        Terminal.WriteErrorLine(
          ".template.config/template.json still declares removed package-mode symbols:".Red());
        foreach (string hit in hits)
          Terminal.WriteErrorLine($"  {hit}");
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine("Removed package symbols absent from template.json.");
      return true;
    }

    /// <summary>
    /// Post-generate: no vendored platform trees; removed *Packages symbols gone; belt-and-suspenders
    /// scan for rewritten {appName}.(Analyzers|Generators|Attributes|TypedIds) including .cs.
    /// Suffixes come from harness props SSOT derivation.
    /// </summary>
    private bool AssertGeneratedAppPackageMode(string appName, string outputDir)
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

      failures.AddRange(Harness.CollectRewrittenPlatformTokenHits(appName, outputDir));

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
    private bool AssertMockAuthFailClosedSurfaces(string appName, string outputDir)
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
        if (System.Text.RegularExpressions.Regex.IsMatch
          (
            text,
            @"""UseMock""\s*:\s*true",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
          ))
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
        if (System.Text.RegularExpressions.Regex.IsMatch
          (
            spaText,
            @"DefineConstants[^>]*MOCK_AUTHENTICATION",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
          ))
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
}
