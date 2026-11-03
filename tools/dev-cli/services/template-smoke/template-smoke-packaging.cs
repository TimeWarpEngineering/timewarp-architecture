#region Purpose
// Packs platform packages and the template into the smoke feed, installs it, writes the local NuGet.config.
#endregion
#region Design
// Smoke-only setup: pack platform packages + template at 2.0.0-smoke into artifacts/template-smoke/packages,
// install the template, and give each generated app a NuGet.config mapping platform packages to the
// local feed (TimeWarp.Contracts still restores from nuget.org). Work dir: artifacts/template-smoke/.
// Prefer `dotnet run tools/dev-cli/dev.cs -- template-smoke` (or re-self-install) over a stale AOT
// `./bin/dev`; CI always uses the runfile path. Process calls are Amuru wrappers of dotnet.
#endregion

namespace DevCli.Services;

using static DevCli.Services.TemplateSmokeMatrix;

/// <summary>
/// Pack, install and feed-configuration steps for template-smoke.
/// </summary>
internal sealed class TemplateSmokePackaging
{
  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private readonly string PackagesDir;
  private readonly string WorkDir;
  private readonly TemplateSmokeHarness Harness;

  public TemplateSmokePackaging(ITerminal terminal, string repoRoot, string packagesDir, string workDir, TemplateSmokeHarness harness)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
    PackagesDir = packagesDir;
    WorkDir = workDir;
    Harness = harness;
  }

  public async Task<bool> PackPlatformPackagesAsync(CancellationToken ct)
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
        .RunAsync(ct);

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

  public async Task<bool> PackTemplateAsync(CancellationToken ct)
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
      .RunAsync(ct);

    if (exitCode != 0)
    {
      Terminal.WriteErrorLine("Template pack failed!".Red());
      Environment.ExitCode = exitCode;
      return false;
    }

    return true;
  }

  public async Task<bool> InstallTemplateAsync(CancellationToken ct)
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
      .CaptureAsync(ct);
    _ = uninstall; // may fail if not installed

    CommandOutput result = await Shell.Builder("dotnet")
      .WithArguments("new", "install", nupkg)
      .WithWorkingDirectory(RepoRoot)
      .WithNoValidation()
      .CaptureAsync(ct);

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

  public void WriteLocalNuGetConfig(string outputDir)
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
    Terminal.WriteLine($"Wrote {configPath} (platform packages → smoke-local; TimeWarp.Contracts → nuget.org).");
  }

  /// <summary>
  /// Monorepo template.json must not declare the removed source-mode package symbols
  /// (generated apps never receive .template.config, so this is the authoritative check).
  /// </summary>
  public bool AssertRemovedPackageSymbolsGoneFromTemplateConfig()
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
}
