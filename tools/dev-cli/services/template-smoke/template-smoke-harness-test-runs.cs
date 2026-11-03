#region Purpose
// TemplateSmokeHarness partial: co-located test, Jaribu aggregator and generated test-suite assertions.
#endregion
#region Design
// Task 135: AssertCoLocatedTestFilesSurviveGeneration (tier 1, cheap) + AssertCoLocatedTestFilesRunAsync
// (tier 2, authoritative `dotnet run`) regression-test the co-located Jaribu runfile JARIBU_MULTI
// template-safety escape (cnd:noEmit) for generated apps - the ordinary solution build never touches
// these files (they compile into no layer project by design), so without this pair the gate would be
// silently blind to a regression here.
// Task 136 / 226 / 228: AssertJaribuFamilyAggregatorsAsync (tier 3) bare `dotnet test -c Release`
// from each family aggregator project dir. Gate is exit 0, failed == 0, total > 0, and
// total == succeeded + skipped. Unparsable MTP summary fails (silent zero-discovery).
// Tiers 2-3 set TIMEWARP_TEST_PORT_BASE so generated-app in-proc hosts do not collide with
// monorepo `dev test` (task 245). Task 330: the value is InProcTestPorts' derived base for the
// generated app's own path (what its tests would pick unset), bumped one slot if it equals this
// checkout's base. The old fixed 17000 collided when two worktrees ran template-smoke at once.
// Aggregators are not in .slnx, so the solution build is also blind to multi-mode compile.
// Task 375: AssertGeneratedAppTestSuitesAsync runs every other generated test project (aspire-tests,
// integration, Playwright, pure suites) under the tier-3 gate. Opt-in (`--app-tests`): the suites
// need Docker and minutes per variant, so CI runs them nightly, not per PR.
#endregion

namespace DevCli.Services;

using static DevCli.Services.TemplateSmokeTestTables;

internal sealed partial class TemplateSmokeHarness
{
  /// <summary>
  /// Tier 1 (cheap): asserts every co-located test file that shipped into the generated app
  /// still contains the JARIBU_MULTI guard lines verbatim. Run right after generation, before
  /// restore/build, so a regression here is reported before spending time on a build.
  /// </summary>
  public bool AssertCoLocatedTestFilesSurviveGeneration(string outputDir, IReadOnlyCollection<string> excludedFamilies)
  {
    bool ok = true;

    foreach ((string family, string relativePath) in CoLocatedTestFiles)
    {
      string generatedPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

      if (excludedFamilies.Contains(family))
      {
        // Flag-off entry: the family's co-located test must NOT ship — its presence means the
        // template's family exclude no longer covers it (orphaned file, broken generated build).
        if (File.Exists(generatedPath))
        {
          Terminal.WriteErrorLine(
            $"{relativePath}: shipped despite --{family} false — template family exclude no longer covers it.".Red());
          ok = false;
        }

        continue;
      }

      if (!File.Exists(generatedPath))
      {
        Terminal.WriteErrorLine($"Co-located test file missing after generation: {relativePath}".Red());
        ok = false;
        continue;
      }

      string text = File.ReadAllText(generatedPath).Replace("\r\n", "\n", StringComparison.Ordinal);
      foreach (string guardLine in JaribuMultiGuardLines)
      {
        if (!text.Contains(guardLine, StringComparison.Ordinal))
        {
          Terminal.WriteErrorLine(
            $"{relativePath}: generated output lost '{guardLine}' — JARIBU_MULTI template-safety guard did not survive dotnet-new generation (M1 regression).".Red());
          ok = false;
        }
      }
    }

    if (ok)
      Terminal.WriteLine("Co-located Jaribu JARIBU_MULTI guard survived generation (tier 1).");

    return ok;
  }

  /// <summary>
  /// In-proc port base for generated-app tier 2/3 hosts (tasks 245, 330): the generated app's
  /// derived base, never equal to this checkout's derived base, so smoke can run beside
  /// monorepo <c>dev test</c> and beside another worktree's smoke.
  /// </summary>
  private string TemplateSmokeInProcTestPortBase(string outputDir)
  {
    int repoBase = InProcTestPorts.DeriveBase(RepoRoot);
    int smokeBase = InProcTestPorts.DeriveBase(outputDir);
    if (smokeBase == repoBase)
      smokeBase = InProcTestPorts.NextDerivedBase(smokeBase);

    return smokeBase.ToString(System.Globalization.CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Tier 2 (authoritative): `dotnet run` each generated co-located runfile standalone and assert
  /// exit 0 with a nonzero, all-passing Jaribu grand total. Proves the file is not just textually
  /// intact but genuinely compiles and runs end to end post-generation (sourceName namespace
  /// rewrite, contract shape, everything). Runs files one at a time (shared smoke port base);
  /// hosts use TemplateSmokeInProcTestPortBase.
  /// </summary>
  public async Task<bool> AssertCoLocatedTestFilesRunAsync(
    string outputDir,
    IReadOnlyCollection<string> excludedFamilies,
    CancellationToken ct)
  {
    bool ok = true;

    foreach ((string family, string relativePath) in CoLocatedTestFiles)
    {
      if (excludedFamilies.Contains(family))
        continue; // absence already asserted by tier 1

      string generatedPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
      if (!File.Exists(generatedPath))
        continue; // already reported by tier 1

      Terminal.WriteLine($"Running generated co-located test standalone: {relativePath}...");

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("run", generatedPath)
        .WithWorkingDirectory(outputDir)
        .WithEnvironmentVariable(InProcTestPorts.EnvironmentVariableName, TemplateSmokeInProcTestPortBase(outputDir))
        .WithNoValidation()
        .CaptureAsync(ct);

      string plain = JaribuOutputParser.StripAnsi(result.Combined);
      bool parsed = JaribuOutputParser.TryParseJaribuGrandTotal(plain, out int total, out int passed);

      if (!result.Success || !parsed || total == 0 || passed != total)
      {
        Terminal.WriteErrorLine(
          $"{relativePath}: generated co-located test did not pass standalone (exit {result.ExitCode}).".Red());
        Terminal.WriteErrorLine(result.Combined);
        ok = false;
        continue;
      }

      Terminal.WriteLine($"{relativePath}: {passed}/{total} passed standalone.".Green());
    }

    return ok;
  }

  /// <summary>
  /// Tier 3 (task 136): assert each family JARIBU_MULTI aggregator exists in the generated app
  /// and that bare <c>dotnet test -c Release</c> from that project directory exits 0, reports
  /// zero failures, a nonzero total, and total == succeeded + skipped. Aggregators are not in
  /// .slnx (solution build never compiles them); this is the multi-mode / MTP regression gate.
  /// Uses TemplateSmokeInProcTestPortBase so hosts do not collide with monorepo <c>dev test</c>
  /// (tasks 245, 330).
  /// </summary>
  public async Task<bool> AssertJaribuFamilyAggregatorsAsync(
    string outputDir,
    IReadOnlyCollection<string> excludedFamilies,
    CancellationToken ct)
  {
    bool ok = true;

    foreach ((string[] requiredFamilies, string relativeProjectDir) in JaribuFamilyAggregators)
    {
      string projectDir = Path.Combine(outputDir, relativeProjectDir.Replace('/', Path.DirectorySeparatorChar));
      string csprojName = Path.GetFileName(relativeProjectDir) + ".csproj";
      string csprojPath = Path.Combine(projectDir, csprojName);

      string[] missingRequiredFamilies = requiredFamilies.Where(excludedFamilies.Contains).ToArray();
      if (missingRequiredFamilies.Length > 0)
      {
        // Flag-off entry: an orphaned aggregator would reference stripped family projects and
        // break the generated app — absence is the pass condition here.
        string flags = string.Join(", ", missingRequiredFamilies.Select(family => $"--{family} false"));
        if (Directory.Exists(projectDir))
        {
          Terminal.WriteErrorLine(
            $"{relativeProjectDir}: generated despite {flags} — template family exclude no longer covers it.".Red());
          ok = false;
        }
        else
        {
          Terminal.WriteLine($"{relativeProjectDir}: correctly excluded ({flags}).");
        }

        continue;
      }

      if (!Directory.Exists(projectDir) || !File.Exists(csprojPath))
      {
        Terminal.WriteErrorLine(
          $"Jaribu family aggregator missing after generation: {relativeProjectDir}/{csprojName}".Red());
        ok = false;
        continue;
      }

      Terminal.WriteLine(
        $"Running generated Jaribu aggregator (MTP bare dotnet test): {relativeProjectDir}...");

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("test", "-c", "Release")
        .WithWorkingDirectory(projectDir)
        .WithEnvironmentVariable(InProcTestPorts.EnvironmentVariableName, TemplateSmokeInProcTestPortBase(outputDir))
        .WithNoValidation()
        .CaptureAsync(ct);

      string plain = JaribuOutputParser.StripAnsi(result.Combined);
      bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(plain, out MtpSummary mtpSummary);
      AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
        parsed ? mtpSummary : null);

      if (!result.Success)
      {
        Terminal.WriteErrorLine(
          $"{relativeProjectDir}: aggregator did not exit 0 (exit {result.ExitCode}).".Red());
        Terminal.WriteErrorLine(result.Combined);
        ok = false;
        continue;
      }

      if (!aggregatorSummaryDecision.Passed)
      {
        string parsedBits = parsed
          ? $"parsed total={mtpSummary.Total}, succeeded={mtpSummary.Succeeded}, failed={mtpSummary.Failed}, skipped={mtpSummary.Skipped}"
          : "unparsable summary";
        Terminal.WriteErrorLine(
          $"{relativeProjectDir}: {aggregatorSummaryDecision.Message} (exit {result.ExitCode}; {parsedBits}).".Red());
        Terminal.WriteErrorLine(result.Combined);
        ok = false;
        continue;
      }

      Terminal.WriteLine(
        $"{relativeProjectDir}: MTP summary total={mtpSummary.Total} succeeded={mtpSummary.Succeeded} skipped={mtpSummary.Skipped}.");
      Terminal.WriteLine($"{relativeProjectDir}: {mtpSummary.Succeeded}/{mtpSummary.Total} succeeded via MTP.".Green());
    }

    return ok;
  }

  /// <summary>
  /// Task 375 (<c>template-smoke --app-tests</c>): bare <c>dotnet test -c Release</c> from every
  /// generated test project directory under tests/, one at a time, with the same MTP summary gate
  /// as tier 3. Skips tier-3 aggregators (already run) and projects declaring
  /// <c>&lt;IsTestProject&gt;false&lt;/IsTestProject&gt;</c> (support libraries). Runs every
  /// project even after a failure, so one run reports every broken suite.
  /// </summary>
  public async Task<bool> AssertGeneratedAppTestSuitesAsync(string outputDir, CancellationToken ct)
  {
    HashSet<string> aggregatorDirs = [.. JaribuFamilyAggregators
      .Select(aggregator => Path.GetFullPath(Path.Combine(outputDir, aggregator.RelativeProjectDir)))];

    string testsDir = Path.Combine(outputDir, "tests");
    string[] projects = Directory.Exists(testsDir)
      ? [.. Directory
          .GetFiles(testsDir, "*.csproj", SearchOption.AllDirectories)
          .Where(project => !TemplateSmokePaths.IsBinObjOrArtifacts(outputDir, project)
                         && !aggregatorDirs.Contains(Path.GetDirectoryName(Path.GetFullPath(project))!)
                         && !File.ReadAllText(project).Contains("<IsTestProject>false</IsTestProject>", StringComparison.Ordinal))
          .Order(StringComparer.Ordinal)]
      : [];

    if (projects.Length == 0)
    {
      Terminal.WriteErrorLine($"No generated test projects found under {testsDir}.".Red());
      return false;
    }

    List<string> failed = [];
    foreach (string project in projects)
    {
      string projectDir = Path.GetDirectoryName(project)!;
      string relativeProjectDir = Path.GetRelativePath(outputDir, projectDir).Replace('\\', '/');
      Terminal.WriteLine($"Running generated test project (MTP bare dotnet test): {relativeProjectDir}...");

      var stopwatch = System.Diagnostics.Stopwatch.StartNew();
      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("test", "-c", "Release")
        .WithWorkingDirectory(projectDir)
        .WithEnvironmentVariable(InProcTestPorts.EnvironmentVariableName, TemplateSmokeInProcTestPortBase(outputDir))
        .WithNoValidation()
        .CaptureAsync(ct);
      string elapsed = stopwatch.Elapsed.TotalSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "s";

      string plain = JaribuOutputParser.StripAnsi(result.Combined);
      bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(plain, out MtpSummary mtpSummary);
      AggregatorSummaryDecision decision = JaribuAggregatorSummaryGate.Decide(parsed ? mtpSummary : null);

      if (!result.Success || !decision.Passed)
      {
        string reason = !result.Success ? $"exit {result.ExitCode}" : decision.Message;
        Terminal.WriteErrorLine($"{relativeProjectDir}: FAILED in {elapsed} ({reason}).".Red());
        Terminal.WriteErrorLine(result.Combined);
        failed.Add(relativeProjectDir);
        continue;
      }

      Terminal.WriteLine($"{relativeProjectDir}: {mtpSummary.Succeeded}/{mtpSummary.Total} succeeded in {elapsed}.".Green());
    }

    if (failed.Count > 0)
    {
      Terminal.WriteErrorLine(
        $"{failed.Count} of {projects.Length} generated test projects failed: {string.Join(", ", failed)}".Red());
      return false;
    }

    Terminal.WriteLine($"All {projects.Length} generated test projects passed.".Green());
    return true;
  }
}
