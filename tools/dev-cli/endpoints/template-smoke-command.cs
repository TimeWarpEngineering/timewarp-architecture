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
// + modules + identity) into a local feed. Published nuget.org pins lag monorepo API surface (e.g.
// Entity<TId>, EndpointAllowAnonymous); smoke validates "this branch's template + this branch's
// packages" by packing at the CPM pin versions and routing TimeWarp.* restores to the local feed
// via packageSourceMapping. Work dir: artifacts/template-smoke/ (under artifacts/).
//
// Task 126-004: generated apps are always package-mode (no foundationPackages/analyzerPackages/
// identityPackages symbols). Vendored platform trees are unconditionally excluded from template
// output. A separate monorepo pre-scan (AssertNoUnsafePlatformNamespaceLiterals) fails on raw
// continuous platform-namespace literals in template-shipped consumer content — including .cs —
// so sourceName cannot rewrite them into the app (historical bug: using TimeWarp.Architecture.TypedIds.Ef
// in postgres-db-context.cs). AssertPackageIdsNotRewritten stays package-id focused and does not
// scan .cs; the namespace-literal pass is independent. Post-generate asserts also confirm vendored
// trees and removed symbols are absent from output.
// Task 126-006: the unsafe-namespace scan set is derived at runtime from property values in
// msbuild/timewarp-platform-packages.props (PackageId AND namespace composition properties). First
// segment after `.Architecture.` becomes a scan suffix (e.g. TypedIds.Ef → TypedIds). Empty
// derivation is a hard failure so props drift cannot silently narrow coverage. Prefer
// `dotnet run tools/dev-cli/dev.cs -- template-smoke` (or re-self-install) over a stale AOT
// `./bin/dev` so local runs pick up these gates; CI always uses the runfile path.
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
    // Identity is always package-mode for generated apps (task 126-004). Without a smoke-local
    // pack, generated apps would pull nuget.org's TimeWarp.Identity whose Foundation dependency
    // floor (>= the published version) collides with the 2.0.0-smoke pins → NU1603 under
    // -warnaserror.
    "source/libraries/timewarp-identity/timewarp-identity.csproj",
  ];

  /// <summary>PackageId substrings whose CPM Version is rewritten to <see cref="SmokePackageVersion"/>.</summary>
  private static readonly string[] SmokePinnedPackageIdFragments =
  [
    "TwArchitectureAnalyzersPackageId",
    "TwArchitectureGeneratorsPackageId",
    "TwArchitectureAttributesPackageId",
    "TimeWarp.Foundation.",
    "TimeWarp.Modules",
    "TimeWarp.Identity",
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
    ".TypedIds",
  ];

  /// <summary>
  /// Extracts the first identifier after <c>.Architecture.</c> from composed props values such as
  /// <c>$(_TwPlatformVendor).Architecture.Analyzers</c> or continuous
  /// <c>TimeWarp.Architecture.TypedIds.Ef</c>.
  /// </summary>
  private static readonly System.Text.RegularExpressions.Regex ComposedArchitectureSuffix =
    new(
      @"(?:\$\(_TwPlatformVendor\)|TimeWarp)\.Architecture\.([A-Za-z_][A-Za-z0-9_]*)",
      System.Text.RegularExpressions.RegexOptions.Compiled);

  private static readonly string[] SourceNameLiteralScanExtensions =
  [
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".razor", ".proto",
  ];

  /// <summary>
  /// Roots under the monorepo that ship into generated apps (or root wiring they inherit).
  /// Platform source trees (foundation/analyzers/libraries) are template-excluded and not scanned.
  /// </summary>
  private static readonly string[] SourceNameLiteralScanRelativeRoots =
  [
    "source/container-apps",
    "tests/common",
    "tests/container-apps",
    "msbuild",
  ];

  private static readonly string[] SourceNameLiteralScanRelativeFiles =
  [
    "Directory.Build.props",
    "Directory.Packages.props",
    "source/Directory.Build.props",
    "tests/Directory.Build.props",
    "timewarp-architecture.slnx",
    "global.json",
    "BannedSymbols.txt",
    "aspire.config.json",
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
    "source/analyzers",
    "tests/foundation",
    "tests/libraries/timewarp-identity-tests",
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

      if (!AssertNoUnsafePlatformNamespaceLiterals()) return Value;
      if (!AssertRemovedPackageSymbolsGoneFromTemplateConfig()) return Value;
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

      if (!AssertGeneratedAppPackageMode(name, outputDir))
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
              <package pattern="TimeWarp.Identity" />
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
    /// Scans monorepo template-shipped consumer content (including .cs) for continuous
    /// TimeWarp.Architecture.&lt;suffix&gt; literals that sourceName would rewrite. Suffix set is
    /// derived from msbuild/timewarp-platform-packages.props (not hand-maintained). Separate from
    /// AssertPackageIdsNotRewritten (which omits .cs by design).
    /// </summary>
    private bool AssertNoUnsafePlatformNamespaceLiterals()
    {
      Terminal.WriteLine("Scanning monorepo template-shipped content for unsafe platform namespace literals...");

      System.Text.RegularExpressions.Regex? unsafeLiteral = BuildUnsafePlatformNamespaceLiteralRegex();
      if (unsafeLiteral is null)
        return false;

      List<string> hits = [];

      foreach (string relativeRoot in SourceNameLiteralScanRelativeRoots)
      {
        string root = Path.Combine(RepoRoot, relativeRoot);
        if (!Directory.Exists(root))
          continue;

        foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
          if (IsBinObjOrArtifacts(RepoRoot, file))
            continue;

          if (!IsSourceNameLiteralScanExtension(file))
            continue;

          CollectUnsafeNamespaceHits(file, Path.GetRelativePath(RepoRoot, file), hits, unsafeLiteral);
        }
      }

      foreach (string relativeFile in SourceNameLiteralScanRelativeFiles)
      {
        string file = Path.Combine(RepoRoot, relativeFile);
        if (!File.Exists(file))
          continue;

        CollectUnsafeNamespaceHits(file, relativeFile, hits, unsafeLiteral);
      }

      if (hits.Count > 0)
      {
        Terminal.WriteErrorLine(
          "Unsafe platform namespace literals (sourceName-rewritable) in template-shipped content:".Red());
        foreach (string hit in hits)
          Terminal.WriteErrorLine($"  {hit}");
        Terminal.WriteErrorLine(
          "Use composed properties from msbuild/timewarp-platform-packages.props (or MSBuild <Using>) instead.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine("Unsafe platform namespace literal scan passed.");
      return true;
    }

    /// <summary>
    /// Builds the continuous-literal scan regex from first segments after <c>.Architecture.</c>
    /// in property values of <c>msbuild/timewarp-platform-packages.props</c>.
    /// </summary>
    private System.Text.RegularExpressions.Regex? BuildUnsafePlatformNamespaceLiteralRegex()
    {
      List<string> suffixes = DeriveUnsafePlatformNamespaceSuffixes();
      if (suffixes.Count == 0)
        return null;

      Terminal.WriteLine(
        $"Derived platform-namespace scan suffixes from timewarp-platform-packages.props: {string.Join(", ", suffixes)}");

      string alternation = string.Join(
        "|",
        suffixes.Select(static suffix => System.Text.RegularExpressions.Regex.Escape(suffix)));

      return new System.Text.RegularExpressions.Regex(
        $@"TimeWarp\.Architecture\.({alternation})\b",
        System.Text.RegularExpressions.RegexOptions.Compiled);
    }

    /// <summary>
    /// Parses composed PackageId and namespace property values; returns distinct first segments
    /// after <c>.Architecture.</c>, ordinal-sorted. Hard-fails (empty list + ExitCode) if the props
    /// file is missing or yields no suffixes.
    /// </summary>
    private List<string> DeriveUnsafePlatformNamespaceSuffixes()
    {
      string propsPath = Path.Combine(RepoRoot, "msbuild", "timewarp-platform-packages.props");
      if (!File.Exists(propsPath))
      {
        Terminal.WriteErrorLine(
          "Cannot derive platform-namespace scan set: msbuild/timewarp-platform-packages.props not found.".Red());
        Environment.ExitCode = 1;
        return [];
      }

      var document = XDocument.Load(propsPath);
      HashSet<string> suffixes = new(StringComparer.Ordinal);

      foreach (XElement propertyGroup in document.Descendants("PropertyGroup"))
      {
        foreach (XElement property in propertyGroup.Elements())
        {
          string value = property.Value.Trim();
          if (value.Length == 0)
            continue;

          System.Text.RegularExpressions.Match match = ComposedArchitectureSuffix.Match(value);
          if (match.Success)
            suffixes.Add(match.Groups[1].Value);
        }
      }

      if (suffixes.Count == 0)
      {
        Terminal.WriteErrorLine(
          "Cannot derive platform-namespace scan set: no .Architecture.<suffix> property values found in msbuild/timewarp-platform-packages.props.".Red());
        Environment.ExitCode = 1;
        return [];
      }

      return suffixes.OrderBy(static suffix => suffix, StringComparer.Ordinal).ToList();
    }

    private static void CollectUnsafeNamespaceHits(
      string file,
      string relativeDisplay,
      List<string> hits,
      System.Text.RegularExpressions.Regex unsafePlatformNamespaceLiteral)
    {
      string text = File.ReadAllText(file);
      foreach (System.Text.RegularExpressions.Match match in unsafePlatformNamespaceLiteral.Matches(text))
      {
        hits.Add($"{relativeDisplay}: '{match.Value}'");
      }
    }

    private static bool IsSourceNameLiteralScanExtension(string file)
    {
      string extension = Path.GetExtension(file);
      return SourceNameLiteralScanExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsBinObjOrArtifacts(string root, string file)
    {
      string relative = Path.GetRelativePath(root, file);
      string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      return parts.Any(part =>
        part.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
        || part.Equals("artifacts", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Post-generate: no vendored platform trees; removed *Packages symbols gone; belt-and-suspenders
    /// scan for rewritten {appName}.(Analyzers|Generators|Attributes|TypedIds) including .cs.
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

      // Optional belt: rewritten continuous platform namespaces in generated tree (incl. .cs).
      string[] rewrittenTokens = ForbiddenRewrittenPackageFragments
        .Select(suffix => appName + suffix)
        .ToArray();

      foreach (string file in Directory.EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories))
      {
        if (IsBinObjOrArtifacts(outputDir, file))
          continue;

        if (!IsSourceNameLiteralScanExtension(file))
          continue;

        string text = File.ReadAllText(file);
        string relative = Path.GetRelativePath(outputDir, file);
        foreach (string token in rewrittenTokens)
        {
          if (text.Contains(token, StringComparison.Ordinal))
            failures.Add($"{relative}: contains rewritten '{token}'");
        }
      }

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

    private bool AssertPackageIdsNotRewritten(string appName, string outputDir)
    {
      // sourceName rewrite of package IDs produces e.g. SmokeDefault.Analyzers — nonexistent.
      // Scoped to MSBuild/JSON (not .cs) — namespace-literal .cs coverage is AssertNoUnsafePlatformNamespaceLiterals.
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
