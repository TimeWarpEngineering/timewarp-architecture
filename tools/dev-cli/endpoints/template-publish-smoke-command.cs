#region Purpose
// Post-publish gate: install the published template from nuget.org, generate, pin-assert, restore+build.
#endregion
#region Design
// Complements `dev template-smoke` (branch-local packs at 2.0.0-smoke). This command never packs —
// it waits for nuget.org flatcontainer availability of the release version, installs
// TimeWarp.Architecture@{version} into an isolated DOTNET_CLI_HOME, generates apps whose names
// ≠ sourceName (so rewrite bugs surface), asserts CPM platform pins == release version, then
// restore+build against nuget.org ONLY (app-local NuGet.config with <clear/>).
//
// Wired as a required step after a real push in `dev workflow` release mode. Pack-only runs
// (no API key) skip the gate. Failure sets Environment.ExitCode nonzero and blocks the release.
// Isolation roots: artifacts/template-publish-smoke/{cli-home,nuget-packages,work}.
// git init + empty commit before build (task 124): TimeWarp.Build.Tasks git-metadata needs a
// real HEAD; init alone still warns on empty repos.
#endregion

namespace DevCli.Commands;

[NuruRoute("template-publish-smoke", Description = "Post-publish gate: generate published template against real nuget.org and build")]
internal sealed class TemplatePublishSmokeCommand : ICommand<Unit>
{
  private const string TemplatePackageId = "TimeWarp.Architecture";
  private const string TemplateShortName = "timewarp-architecture";

  private static readonly string[] RequiredPublishedPackageIds =
  [
    "TimeWarp.Architecture",
    "TimeWarp.Architecture.Analyzers",
    "TimeWarp.Architecture.Generators",
    "TimeWarp.Architecture.Attributes",
    "TimeWarp.Foundation.Domain",
    "TimeWarp.Foundation.Contracts",
    "TimeWarp.Foundation.Application",
    "TimeWarp.Foundation.Infrastructure",
    "TimeWarp.Foundation.Server",
    "TimeWarp.Modules",
    "TimeWarp.Identity",
  ];

  private static readonly (string Name, string[] ExtraArgs)[] SmokeMatrix =
  [
    ("PublishSmokeDefault", []),
    ("PublishSmokeNoPostgres", ["--postgres", "false"]),
  ];

  private static readonly string[] PlatformPinIncludeFragments =
  [
    "TwArchitectureAnalyzersPackageId",
    "TwArchitectureGeneratorsPackageId",
    "TwArchitectureAttributesPackageId",
    "TimeWarp.Foundation.",
    "TimeWarp.Modules",
    "TimeWarp.Identity",
  ];

  private static readonly string[] ForbiddenRewrittenPackageFragments =
  [
    ".Analyzers",
    ".Generators",
    ".Attributes",
    ".TypedIds",
  ];

  [Option("version", "v", Description = "Published version to validate (default: source/Directory.Build.props <Version>)")]
  public string? Version { get; set; }

  [Option("skip-wait", Description = "Skip flatcontainer availability wait (packages already known present)")]
  public bool SkipWait { get; set; }

  internal sealed class Handler : ICommandHandler<TemplatePublishSmokeCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;
    private string SmokeRoot = null!;
    private string CliHomeDir = null!;
    private string NugetPackagesDir = null!;
    private string WorkDir = null!;
    private string Version = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TemplatePublishSmokeCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;

      string? version = command.Version ?? ReadReleaseVersion();
      if (string.IsNullOrWhiteSpace(version))
      {
        Terminal.WriteErrorLine("Error: could not resolve version (pass --version or set source/Directory.Build.props <Version>).".Red());
        Environment.ExitCode = 1;
        return Value;
      }

      Version = version.Trim();
      SmokeRoot = Path.Combine(RepoRoot, "artifacts", "template-publish-smoke");
      CliHomeDir = Path.Combine(SmokeRoot, "cli-home");
      NugetPackagesDir = Path.Combine(SmokeRoot, "nuget-packages");
      WorkDir = Path.Combine(SmokeRoot, "work");

      Terminal.WriteLine($"\nTemplate publish smoke — version {Version}".Cyan());
      Terminal.WriteLine($"Work root: {SmokeRoot}\n");

      PrepareArtifactDirs();

      if (!AssertPinLogicSelfCheck()) return Value;

      if (!command.SkipWait)
      {
        if (!await WaitForFlatcontainerAsync()) return Value;
      }
      else
      {
        Terminal.WriteLine("Skipping flatcontainer wait (--skip-wait).".Yellow());
      }

      if (!await InstallPublishedTemplateAsync()) return Value;

      foreach ((string name, string[] extraArgs) in SmokeMatrix)
      {
        if (!await SmokeOneAsync(name, extraArgs))
        {
          Terminal.WriteErrorLine($"\nTemplate publish smoke FAILED — {name}".Red());
          Environment.ExitCode = 1;
          return Value;
        }
      }

      Terminal.WriteLine("\nTemplate publish smoke SUCCEEDED".Green());
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

    private string? ReadReleaseVersion()
    {
      string propsPath = Path.Combine(RepoRoot, "source", "Directory.Build.props");
      if (!File.Exists(propsPath))
        return null;

      var doc = XDocument.Load(propsPath);
      XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
      XElement? versionElement = doc.Descendants(ns + "Version").FirstOrDefault()
        ?? doc.Descendants("Version").FirstOrDefault();
      return versionElement?.Value?.Trim();
    }

    private void PrepareArtifactDirs()
    {
      if (Directory.Exists(SmokeRoot))
        Directory.Delete(SmokeRoot, recursive: true);

      Directory.CreateDirectory(CliHomeDir);
      Directory.CreateDirectory(NugetPackagesDir);
      Directory.CreateDirectory(WorkDir);
      Terminal.WriteLine($"Prepared isolation dirs under {SmokeRoot}.");
    }

    /// <summary>
    /// Always-on: synthetic Directory.Packages.props must pass with matching pins and fail with
    /// stale or zero platform pins — proves the assert logic itself before relying on it.
    /// </summary>
    private bool AssertPinLogicSelfCheck()
    {
      Terminal.WriteLine("Pin-assert self-check...");

      const string expected = "2.0.0-beta.99";
      string good =
        $"""
        <Project>
          <ItemGroup>
            <PackageVersion Include="$(TwArchitectureAnalyzersPackageId)" Version="{expected}" />
            <PackageVersion Include="$(TwArchitectureGeneratorsPackageId)" Version="{expected}" />
            <PackageVersion Include="$(TwArchitectureAttributesPackageId)" Version="{expected}" />
            <PackageVersion Include="TimeWarp.Foundation.Domain" Version="{expected}" />
            <PackageVersion Include="TimeWarp.Modules" Version="{expected}" />
            <PackageVersion Include="TimeWarp.Identity" Version="{expected}" />
            <PackageVersion Include="SomeOther.Package" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

      string stale = good.Replace(expected, "2.0.0-beta.1", StringComparison.Ordinal);
      string noPlatformPins =
        """
        <Project>
          <ItemGroup>
            <PackageVersion Include="SomeOther.Package" Version="1.0.0" />
          </ItemGroup>
        </Project>
        """;

      if (!TryEvaluatePlatformPins(good, expected, out List<string> goodFailures, out int goodCount)
          || goodCount == 0
          || goodFailures.Count > 0)
      {
        Terminal.WriteErrorLine("Pin-assert self-check failed: correct pins should pass.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      if (TryEvaluatePlatformPins(stale, expected, out _, out _))
      {
        Terminal.WriteErrorLine("Pin-assert self-check failed: stale pins should fail.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      // Zero platform pins: Evaluate returns false when pinCount == 0.
      if (TryEvaluatePlatformPins(noPlatformPins, expected, out _, out int emptyCount) || emptyCount != 0)
      {
        Terminal.WriteErrorLine("Pin-assert self-check failed: zero platform pins should hard-fail.".Red());
        Environment.ExitCode = 1;
        return false;
      }

      Terminal.WriteLine("Pin-assert self-check passed.");
      return true;
    }

    private async Task<bool> WaitForFlatcontainerAsync()
    {
      Terminal.WriteLine($"Waiting for nuget.org flatcontainer availability of {Version} (budget 12 min)...");

      using System.Net.Http.HttpClient http = new();
      http.Timeout = TimeSpan.FromSeconds(30);

      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(12);
      int delaySeconds = 5;
      int attempt = 0;

      while (true)
      {
        attempt++;
        List<string> missing = [];

        foreach (string packageId in RequiredPublishedPackageIds)
        {
          var nupkgUri = new Uri(FlatcontainerNupkgUrl(packageId, Version));
          try
          {
            using System.Net.Http.HttpResponseMessage response =
              await http.GetAsync(nupkgUri, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, Ct);
            if (!response.IsSuccessStatusCode)
              missing.Add(packageId);
          }
          catch (Exception ex) when (ex is not OperationCanceledException)
          {
            missing.Add(packageId);
          }
        }

        if (missing.Count == 0)
        {
          Terminal.WriteLine(
            $"All {RequiredPublishedPackageIds.Length} packages available on flatcontainer (attempt {attempt}).".Green());
          return true;
        }

        if (DateTimeOffset.UtcNow >= deadline)
        {
          Terminal.WriteErrorLine(
            $"Flatcontainer wait timed out after 12 min. Still missing: {string.Join(", ", missing)}".Red());
          Environment.ExitCode = 1;
          return false;
        }

        Terminal.WriteLine(
          $"  attempt {attempt}: {missing.Count} missing ({string.Join(", ", missing)}) — retry in {delaySeconds}s...");

        try
        {
          await Task.Delay(TimeSpan.FromSeconds(delaySeconds), Ct);
        }
        catch (OperationCanceledException)
        {
          Environment.ExitCode = 1;
          return false;
        }

        delaySeconds = Math.Min(delaySeconds * 2, 60);
      }
    }

    private static string FlatcontainerNupkgUrl(string packageId, string version)
    {
      string id = packageId.ToLowerInvariant();
      string ver = version.ToLowerInvariant();
      return $"https://api.nuget.org/v3-flatcontainer/{id}/{ver}/{id}.{ver}.nupkg";
    }

    private async Task<bool> InstallPublishedTemplateAsync()
    {
      // Prefer @ separator (dotnet new deprecates ::).
      string packageRef = $"{TemplatePackageId}@{Version}";
      Terminal.WriteLine($"Installing published template {packageRef} (isolated hive)...");

      // Force reinstall so an older conflicting identity does not win.
      CommandOutput uninstall = await Shell.Builder("dotnet")
        .WithArguments("new", "uninstall", TemplatePackageId)
        .WithWorkingDirectory(RepoRoot)
        .WithEnvironmentVariable("DOTNET_CLI_HOME", CliHomeDir)
        .WithEnvironmentVariable("NUGET_PACKAGES", NugetPackagesDir)
        .WithNoValidation()
        .CaptureAsync(Ct);
      _ = uninstall; // may fail if not installed

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("new", "install", packageRef)
        .WithWorkingDirectory(RepoRoot)
        .WithEnvironmentVariable("DOTNET_CLI_HOME", CliHomeDir)
        .WithEnvironmentVariable("NUGET_PACKAGES", NugetPackagesDir)
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
        "new", TemplateShortName,
        "-n", name,
        "-o", outputDir,
        "--force",
      ];
      args.AddRange(extraArgs);

      CommandOutput generate = await Shell.Builder("dotnet")
        .WithArguments([.. args])
        .WithWorkingDirectory(RepoRoot)
        .WithEnvironmentVariable("DOTNET_CLI_HOME", CliHomeDir)
        .WithEnvironmentVariable("NUGET_PACKAGES", NugetPackagesDir)
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

      if (!AssertPlatformPinsEqualReleaseVersion(outputDir))
        return false;

      string platformProps = Path.Combine(outputDir, "msbuild", "timewarp-platform-packages.props");
      if (!File.Exists(platformProps))
      {
        Terminal.WriteErrorLine($"Missing {platformProps} — platform package ID composition file not in template output.".Red());
        return false;
      }

      WriteNugetOrgOnlyConfig(outputDir);

      if (!await GitInitAsync(outputDir))
        return false;

      string? solution = Directory
        .GetFiles(outputDir, "*.slnx", SearchOption.TopDirectoryOnly)
        .Concat(Directory.GetFiles(outputDir, "*.sln", SearchOption.TopDirectoryOnly))
        .FirstOrDefault();

      if (solution is null)
      {
        Terminal.WriteErrorLine($"No solution file found under {outputDir}.".Red());
        return false;
      }

      Terminal.WriteLine($"Restoring {solution} (nuget.org only)...");
      int restoreExit = await DotNet.Restore()
        .WithProject(solution)
        .WithWorkingDirectory(outputDir)
        .WithEnvironmentVariable("DOTNET_CLI_HOME", CliHomeDir)
        .WithEnvironmentVariable("NUGET_PACKAGES", NugetPackagesDir)
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
        .WithEnvironmentVariable("DOTNET_CLI_HOME", CliHomeDir)
        .WithEnvironmentVariable("NUGET_PACKAGES", NugetPackagesDir)
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

    private async Task<bool> GitInitAsync(string outputDir)
    {
      // Task 124: TimeWarp.Build.Tasks git-metadata needs a real HEAD (init alone still warns on
      // empty repos). Local identity via -c so we never touch the developer's git config.
      CommandOutput init = await Shell.Builder("git")
        .WithArguments("init")
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!init.Success)
      {
        Terminal.WriteErrorLine(init.Combined);
        Terminal.WriteErrorLine($"git init failed in {outputDir}.".Red());
        return false;
      }

      CommandOutput commit = await Shell.Builder("git")
        .WithArguments(
          "-c", "user.email=template-publish-smoke@local",
          "-c", "user.name=template-publish-smoke",
          "commit",
          "--allow-empty",
          "-m", "template-publish-smoke")
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (!commit.Success)
      {
        Terminal.WriteErrorLine(commit.Combined);
        Terminal.WriteErrorLine($"git commit failed in {outputDir}.".Red());
        return false;
      }

      Terminal.WriteLine($"git init + empty commit in {outputDir}.");
      return true;
    }

    private void WriteNugetOrgOnlyConfig(string outputDir)
    {
      string configPath = Path.Combine(outputDir, "NuGet.config");
      string xml =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
          </packageSources>
        </configuration>
        """;
      File.WriteAllText(configPath, xml);
      Terminal.WriteLine($"Wrote {configPath} (nuget.org only).");
    }

    private bool AssertPlatformPinsEqualReleaseVersion(string outputDir)
    {
      string packagesProps = Path.Combine(outputDir, "Directory.Packages.props");
      if (!File.Exists(packagesProps))
      {
        Terminal.WriteErrorLine($"Missing {packagesProps}.".Red());
        return false;
      }

      string text = File.ReadAllText(packagesProps);
      if (!TryEvaluatePlatformPins(text, Version, out List<string> failures, out int pinCount))
      {
        if (pinCount == 0)
        {
          Terminal.WriteErrorLine(
            "Directory.Packages.props: zero platform PackageVersion pins matched — pattern/fragment drift?".Red());
        }
        else
        {
          Terminal.WriteErrorLine(
            $"Directory.Packages.props: platform pins must equal release version {Version}:".Red());
          foreach (string failure in failures)
            Terminal.WriteErrorLine($"  {failure}");
        }

        return false;
      }

      Terminal.WriteLine($"Platform CPM pins == {Version} ({pinCount} pins).");
      return true;
    }

    /// <summary>
    /// Returns true only when at least one platform pin was found and every platform pin's Version
    /// equals <paramref name="expectedVersion"/>.
    /// </summary>
    private static bool TryEvaluatePlatformPins(
      string packagesPropsText,
      string expectedVersion,
      out List<string> failures,
      out int pinCount)
    {
      failures = [];
      pinCount = 0;

      System.Text.RegularExpressions.MatchCollection matches =
        System.Text.RegularExpressions.Regex.Matches(
          packagesPropsText,
          "PackageVersion\\s+Include=\"([^\"]+)\"\\s+Version=\"([^\"]+)\"");

      foreach (System.Text.RegularExpressions.Match match in matches)
      {
        string include = match.Groups[1].Value;
        string pinVersion = match.Groups[2].Value;
        bool isPlatform = PlatformPinIncludeFragments.Any(fragment =>
          include.Contains(fragment, StringComparison.Ordinal));
        if (!isPlatform)
          continue;

        pinCount++;
        if (!string.Equals(pinVersion, expectedVersion, StringComparison.Ordinal))
          failures.Add($"{include}: Version=\"{pinVersion}\" (expected \"{expectedVersion}\")");
      }

      return pinCount > 0 && failures.Count == 0;
    }

    private bool AssertPackageIdsNotRewritten(string appName, string outputDir)
    {
      // sourceName rewrite of package IDs produces e.g. PublishSmokeDefault.Analyzers — nonexistent.
      // Scoped to MSBuild/JSON (not .cs) — same policy as template-smoke.
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
