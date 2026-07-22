#region Purpose
// Pack, install, generate, and build the template to catch sourceName/package dual-mode regressions.
#endregion
#region Design
// Regression gate for task 115: template sourceName is TimeWarp.Architecture, so package-id
// literals TimeWarp.Architecture.* used to rewrite to SmokeDefault.* and break restore. Smoke
// always generates with names ≠ sourceName (SmokeDefault, SmokeNoPostgres) so rewrite bugs
// surface. Matrix: defaults + --postgres false. Asserts composed platform package IDs survived
// generation, then restore + build -warnaserror (TreatWarningsAsErrors already on).
//
// Packs the template AND monorepo platform packages (foundation + analyzers/generators/attributes
// + modules) into a local feed. Published nuget.org pins lag monorepo API surface (e.g. Entity<TId>,
// EndpointAllowAnonymous); smoke validates "this branch's template + this branch's packages" by
// packing at the CPM pin versions and routing TimeWarp.* restores to the local feed via
// packageSourceMapping. Work dir: artifacts/template-smoke/ (under artifacts/).
#endregion

namespace DevCli.Commands;

[NuruRoute("template-smoke", Description = "Pack/install template and smoke-build generated apps (defaults + --postgres false)")]
internal sealed class TemplateSmokeCommand : ICommand<Unit>
{
  private const string TemplateProject =
    "timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj";

  private const string TemplatePackageId = "TimeWarp.Architecture";

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
  ];

  /// <summary>PackageId substrings whose CPM Version is rewritten to <see cref="SmokePackageVersion"/>.</summary>
  private static readonly string[] SmokePinnedPackageIdFragments =
  [
    "TwArchitectureAnalyzersPackageId",
    "TwArchitectureGeneratorsPackageId",
    "TwArchitectureAttributesPackageId",
    "TimeWarp.Foundation.",
    "TimeWarp.Modules",
  ];

  private static readonly (string Name, string[] ExtraArgs)[] SmokeMatrix =
  [
    ("SmokeDefault", []),
    ("SmokeNoPostgres", ["--postgres", "false"]),
  ];

  private static readonly string[] ForbiddenRewrittenPackageFragments =
  [
    ".Analyzers",
    ".Generators",
    ".Attributes",
  ];

  internal sealed class Handler : ICommandHandler<TemplateSmokeCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string SmokeRoot = null!;
    private string PackagesDir = null!;
    private string WorkDir = null!;

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

      Terminal.WriteLine($"\nTemplate smoke — work root: {SmokeRoot}\n".Cyan());

      if (!await PackPlatformPackagesAsync()) return Value;
      if (!await PackTemplateAsync()) return Value;
      if (!await InstallTemplateAsync()) return Value;

      foreach ((string name, string[] extraArgs) in SmokeMatrix)
      {
        if (!await SmokeOneAsync(name, extraArgs))
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

      return true;
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
      string? nupkg = Directory
        .GetFiles(PackagesDir, $"{TemplatePackageId}.*.nupkg")
        .Where(p => !p.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)
                 && !Path.GetFileName(p).Contains(".Analyzers.", StringComparison.Ordinal)
                 && !Path.GetFileName(p).Contains(".Generators.", StringComparison.Ordinal)
                 && !Path.GetFileName(p).Contains(".Attributes.", StringComparison.Ordinal))
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

    private async Task<bool> SmokeOneAsync(string name, string[] extraArgs)
    {
      string outputDir = Path.Combine(WorkDir, name);
      Terminal.WriteLine($"\n── Generate {name} ──".Cyan());

      List<string> args =
      [
        "new", "timewarp-architecture",
        "-n", name,
        "-o", outputDir,
        "--force",
      ];
      args.AddRange(extraArgs);

      CommandOutput generate = await Shell.Builder("dotnet")
        .WithArguments([.. args])
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!generate.Success)
      {
        Terminal.WriteErrorLine(generate.Combined);
        Terminal.WriteErrorLine($"dotnet new failed for {name}!".Red());
        return false;
      }

      Terminal.WriteLine(generate.Combined);

      if (!AssertPackageIdsNotRewritten(name, outputDir))
        return false;

      if (!RewriteCpmPinsToSmokeVersion(outputDir))
        return false;

      WriteLocalNuGetConfig(outputDir);

      string? solution = Directory
        .GetFiles(outputDir, "*.slnx", SearchOption.TopDirectoryOnly)
        .Concat(Directory.GetFiles(outputDir, "*.sln", SearchOption.TopDirectoryOnly))
        .FirstOrDefault();

      if (solution is null)
      {
        Terminal.WriteErrorLine($"No solution file found under {outputDir}.".Red());
        return false;
      }

      Terminal.WriteLine($"Restoring {solution} (local TimeWarp.* feed)...");
      int restoreExit = await DotNet.Restore()
        .WithProject(solution)
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .RunAsync(Ct);
      if (restoreExit != 0)
      {
        Terminal.WriteErrorLine($"Restore failed for {name}!".Red());
        return false;
      }

      Terminal.WriteLine($"Building {solution} (Release)...");
      int buildExit = await DotNet.Build()
        .WithProject(solution)
        .WithConfiguration("Release")
        .WithNoRestore()
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .RunAsync(Ct);
      if (buildExit != 0)
      {
        Terminal.WriteErrorLine($"Build failed for {name}!".Red());
        return false;
      }

      Terminal.WriteLine($"{name} OK".Green());
      return true;
    }

    private bool RewriteCpmPinsToSmokeVersion(string outputDir)
    {
      string packagesProps = Path.Combine(outputDir, "Directory.Packages.props");
      if (!File.Exists(packagesProps))
      {
        Terminal.WriteErrorLine($"Missing {packagesProps}.".Red());
        return false;
      }

      string text = File.ReadAllText(packagesProps);
      string original = text;

      // Match PackageVersion Include="..." Version="..." for platform packages only.
      // Property-composed analyzer IDs appear as Include="$(TwArchitecture…PackageId)".
      text = System.Text.RegularExpressions.Regex.Replace(
        text,
        "PackageVersion\\s+Include=\"([^\"]+)\"\\s+Version=\"[^\"]+\"",
        match =>
        {
          string include = match.Groups[1].Value;
          bool isPlatform = SmokePinnedPackageIdFragments.Any(fragment =>
            include.Contains(fragment, StringComparison.Ordinal));
          if (!isPlatform)
            return match.Value;
          return $"PackageVersion Include=\"{include}\" Version=\"{SmokePackageVersion}\"";
        });

      if (text == original)
      {
        Terminal.WriteErrorLine("Directory.Packages.props: no platform PackageVersion pins rewritten — pattern mismatch?".Red());
        return false;
      }

      File.WriteAllText(packagesProps, text);
      Terminal.WriteLine($"Rewrote platform CPM pins → {SmokePackageVersion}.");
      return true;
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

    private bool AssertPackageIdsNotRewritten(string appName, string outputDir)
    {
      // sourceName rewrite of package IDs produces e.g. SmokeDefault.Analyzers — nonexistent.
      string[] forbidden =
        ForbiddenRewrittenPackageFragments
          .Select(suffix => appName + suffix)
          .ToArray();

      List<string> hits = [];
      foreach (string file in Directory.EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories))
      {
        string relative = Path.GetRelativePath(outputDir, file);
        if (relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
          continue;
        }

        string extension = Path.GetExtension(file);
        if (extension is not (".props" or ".csproj" or ".targets" or ".slnx" or ".json"))
          continue;

        string text = File.ReadAllText(file);
        foreach (string token in forbidden)
        {
          if (text.Contains(token, StringComparison.Ordinal))
            hits.Add($"{relative}: contains '{token}'");
        }
      }

      string platformProps = Path.Combine(outputDir, "msbuild", "timewarp-platform-packages.props");
      if (!File.Exists(platformProps))
      {
        Terminal.WriteErrorLine($"Missing {platformProps} — platform package ID composition file not packed.".Red());
        return false;
      }

      string propsText = File.ReadAllText(platformProps);
      if (!propsText.Contains("_TwPlatformVendor", StringComparison.Ordinal)
          || !propsText.Contains("TwArchitectureAnalyzersPackageId", StringComparison.Ordinal))
      {
        Terminal.WriteErrorLine("timewarp-platform-packages.props missing expected property names.".Red());
        return false;
      }

      if (hits.Count > 0)
      {
        Terminal.WriteErrorLine("Platform package IDs were rewritten by template sourceName:".Red());
        foreach (string hit in hits)
          Terminal.WriteErrorLine($"  {hit}");
        return false;
      }

      Terminal.WriteLine("Package ID rewrite check passed.");
      return true;
    }
  }
}
