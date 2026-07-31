#region Purpose
// Shared template smoke/publish-smoke harness: props SSOT suffix derivation, rewrite asserts, pin helpers, generate/restore/build skeleton.
#endregion
#region Design
// Task 131-003 / F-007: collapse duplication between template-smoke and template-publish-smoke.
// All rewrite-scan suffixes and nupkg-exclude fragments derive from
// msbuild/timewarp-platform-packages.props (126-006 style) — no hand ForbiddenRewritten arrays.
// Platform CPM pin matching uses one PlatformPinIncludeFragments list (TwArchitecture* +
// Foundation/Modules/Identity). Minimal suffix set (Analyzers/Attributes/Generators/TypedIds)
// is fail-closed so props drift cannot silently narrow gates. All tree walks use
// TemplateSmokePaths.IsBinObjOrArtifacts (bin/obj/artifacts, case-insensitive).
// SmokeOneAsync is the shared generate → package-id assert → callback → restore → build path;
// smoke-only package-mode / local-feed setup and publish-only pin/git/nuget.org steps stay as
// AfterGenerateAsync callbacks on the commands.
// Task 135: AssertCoLocatedTestFilesSurviveGeneration (tier 1, cheap) + AssertCoLocatedTestFilesRunAsync
// (tier 2, authoritative `dotnet run`) regression-test the co-located Jaribu runfile JARIBU_MULTI
// template-safety escape (cnd:noEmit) for generated apps — the ordinary solution build never
// touches these files (they compile into no layer project by design), so without this pair the
// gate would be silently blind to a regression here.
// Task 136: AssertJaribuFamilyAggregatorsAsync (tier 3) bare `dotnet test -c Release` from each
// family aggregator project dir in the generated app (web 5, api 2); serial for fixed port 7255.
// Aggregators are not in .slnx, so the solution build is also blind to multi-mode compile.
#endregion

namespace DevCli.Services;

/// <summary>
/// Path helpers for smoke tree walks.
/// </summary>
internal static class TemplateSmokePaths
{
  /// <summary>
  /// True when <paramref name="file"/> is under bin/, obj/, or artifacts/ relative to
  /// <paramref name="root"/> (any path segment, case-insensitive).
  /// </summary>
  public static bool IsBinObjOrArtifacts(string root, string file)
  {
    string relative = Path.GetRelativePath(root, file);
    string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Any(part =>
      part.Equals("bin", StringComparison.OrdinalIgnoreCase)
      || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
      || part.Equals("artifacts", StringComparison.OrdinalIgnoreCase));
  }
}

/// <summary>
/// Derives Architecture.* first-segment suffixes from timewarp-platform-packages.props SSOT.
/// </summary>
internal static class PlatformPackagePropsSsot
{
  /// <summary>
  /// Extracts the first identifier after <c>.Architecture.</c> from composed props values such as
  /// <c>$(_TwPlatformVendor).Architecture.Analyzers</c> or continuous
  /// <c>TimeWarp.Architecture.TypedIds.Ef</c>.
  /// </summary>
  public static readonly System.Text.RegularExpressions.Regex ComposedArchitectureSuffix =
    new(
      @"(?:\$\(_TwPlatformVendor\)|TimeWarp)\.Architecture\.([A-Za-z_][A-Za-z0-9_]*)",
      System.Text.RegularExpressions.RegexOptions.Compiled);

  /// <summary>
  /// Every derivation must include these first segments or the gate hard-fails (props drift).
  /// </summary>
  public static readonly string[] MinimalRequiredSuffixes =
  [
    "Analyzers",
    "Attributes",
    "Generators",
    "TypedIds",
  ];

  public static string PropsRelativePath => Path.Combine("msbuild", "timewarp-platform-packages.props");

  /// <summary>
  /// Parses composed PackageId and namespace property values; returns distinct first segments
  /// after <c>.Architecture.</c>, ordinal-sorted. On failure sets <paramref name="error"/> and
  /// returns false (empty <paramref name="suffixes"/>).
  /// </summary>
  public static bool TryDeriveArchitectureSuffixes(
    string repoRoot,
    out IReadOnlyList<string> suffixes,
    out string? error)
  {
    suffixes = [];
    error = null;

    string propsPath = Path.Combine(repoRoot, PropsRelativePath);
    if (!File.Exists(propsPath))
    {
      error =
        "Cannot derive platform-namespace scan set: msbuild/timewarp-platform-packages.props not found.";
      return false;
    }

    var document = XDocument.Load(propsPath);
    var found = new HashSet<string>(StringComparer.Ordinal);

    foreach (XElement propertyGroup in document.Descendants("PropertyGroup"))
    {
      foreach (XElement property in propertyGroup.Elements())
      {
        string value = property.Value.Trim();
        if (value.Length == 0)
          continue;

        System.Text.RegularExpressions.Match match = ComposedArchitectureSuffix.Match(value);
        if (match.Success)
          found.Add(match.Groups[1].Value);
      }
    }

    if (found.Count == 0)
    {
      error =
        "Cannot derive platform-namespace scan set: no .Architecture.<suffix> property values found in msbuild/timewarp-platform-packages.props.";
      return false;
    }

    var missingMinimal = MinimalRequiredSuffixes
      .Where(required => !found.Contains(required))
      .ToList();
    if (missingMinimal.Count > 0)
    {
      error =
        "Platform-namespace scan set missing required suffixes from props SSOT: "
        + string.Join(", ", missingMinimal)
        + ". Derived: "
        + string.Join(", ", found.OrderBy(static s => s, StringComparer.Ordinal));
      return false;
    }

    suffixes = found.OrderBy(static suffix => suffix, StringComparer.Ordinal).ToList();
    return true;
  }

  public static IReadOnlyList<string> ToForbiddenRewrittenFragments(IReadOnlyList<string> suffixes) =>
    suffixes.Select(static suffix => "." + suffix).ToArray();

  /// <summary>
  /// Fragments for nupkg filename exclusion (e.g. <c>.Analyzers.</c>) so
  /// TimeWarp.Architecture.Analyzers.*.nupkg is not mistaken for the template package.
  /// </summary>
  public static IReadOnlyList<string> ToNupkgExcludeFragments(IReadOnlyList<string> suffixes) =>
    suffixes.Select(static suffix => "." + suffix + ".").ToArray();
}

/// <summary>
/// Shared assert helpers + generate/restore/build skeleton for template smoke gates.
/// </summary>
internal sealed class TemplateSmokeHarness
{
  /// <summary>
  /// PackageVersion Include fragments treated as platform pins (rewrite / equality asserts).
  /// Hand list by design — not full props derivation (task 131-003).
  /// </summary>
  public static readonly string[] PlatformPinIncludeFragments =
  [
    "TwArchitectureAnalyzersPackageId",
    "TwArchitectureGeneratorsPackageId",
    "TwArchitectureAttributesPackageId",
    "TimeWarp.Foundation.",
    "TimeWarp.Modules",
    "TimeWarp.Identity",
  ];

  private static readonly string[] SourceNameLiteralScanExtensions =
  [
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".razor", ".proto",
  ];

  private static readonly string[] PackageIdRewriteScanExtensions =
  [
    ".props", ".csproj", ".targets", ".slnx", ".json",
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

  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private IReadOnlyList<string>? ArchitectureSuffixes;

  public TemplateSmokeHarness(ITerminal terminal, string repoRoot)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
  }

  /// <summary>
  /// Ensures suffixes are derived and cached. Fail-closed (ExitCode + error line) on props drift.
  /// </summary>
  public bool TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes)
  {
    if (ArchitectureSuffixes is not null)
    {
      suffixes = ArchitectureSuffixes;
      return true;
    }

    if (!PlatformPackagePropsSsot.TryDeriveArchitectureSuffixes(RepoRoot, out suffixes, out string? error))
    {
      Terminal.WriteErrorLine((error ?? "Failed to derive architecture suffixes from props SSOT.").Red());
      Environment.ExitCode = 1;
      suffixes = [];
      return false;
    }

    ArchitectureSuffixes = suffixes;
    return true;
  }

  public IReadOnlyList<string> GetForbiddenRewrittenFragments()
  {
    if (!TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes))
      return [];
    return PlatformPackagePropsSsot.ToForbiddenRewrittenFragments(suffixes);
  }

  public IReadOnlyList<string> GetNupkgExcludeFragments()
  {
    if (!TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes))
      return [];
    return PlatformPackagePropsSsot.ToNupkgExcludeFragments(suffixes);
  }

  /// <summary>
  /// True when the nupkg file name is a platform sibling of the template package
  /// (e.g. TimeWarp.Architecture.Analyzers.*.nupkg).
  /// </summary>
  public bool IsExcludedPlatformNupkgFileName(string fileName)
  {
    IReadOnlyList<string> fragments = GetNupkgExcludeFragments();
    return fragments.Any(fragment =>
      fileName.Contains(fragment, StringComparison.Ordinal));
  }

  public static bool IsSourceNameLiteralScanExtension(string file)
  {
    string extension = Path.GetExtension(file);
    return SourceNameLiteralScanExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
  }

  public static bool IsPlatformPinInclude(string include) =>
    PlatformPinIncludeFragments.Any(fragment =>
      include.Contains(fragment, StringComparison.Ordinal));

  /// <summary>
  /// Returns true only when at least one platform pin was found and every platform pin's Version
  /// equals <paramref name="expectedVersion"/>.
  /// </summary>
  public static bool TryEvaluatePlatformPins(
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
      if (!IsPlatformPinInclude(include))
        continue;

      pinCount++;
      if (!string.Equals(pinVersion, expectedVersion, StringComparison.Ordinal))
        failures.Add($"{include}: Version=\"{pinVersion}\" (expected \"{expectedVersion}\")");
    }

    return pinCount > 0 && failures.Count == 0;
  }

  /// <summary>
  /// Rewrites platform PackageVersion pins in generated Directory.Packages.props to
  /// <paramref name="targetVersion"/>. Hard-fails if zero platform pins matched.
  /// </summary>
  public bool RewriteCpmPins(string outputDir, string targetVersion)
  {
    string packagesProps = Path.Combine(outputDir, "Directory.Packages.props");
    if (!File.Exists(packagesProps))
    {
      Terminal.WriteErrorLine($"Missing {packagesProps}.".Red());
      return false;
    }

    string text = File.ReadAllText(packagesProps);
    string original = text;

    text = System.Text.RegularExpressions.Regex.Replace(
      text,
      "PackageVersion\\s+Include=\"([^\"]+)\"\\s+Version=\"[^\"]+\"",
      match =>
      {
        string include = match.Groups[1].Value;
        if (!IsPlatformPinInclude(include))
          return match.Value;
        return $"PackageVersion Include=\"{include}\" Version=\"{targetVersion}\"";
      });

    if (text == original)
    {
      Terminal.WriteErrorLine(
        "Directory.Packages.props: no platform PackageVersion pins rewritten — pattern mismatch?".Red());
      return false;
    }

    File.WriteAllText(packagesProps, text);
    Terminal.WriteLine($"Rewrote platform CPM pins → {targetVersion}.");
    return true;
  }

  public bool AssertPlatformPinsEqualVersion(string outputDir, string expectedVersion)
  {
    string packagesProps = Path.Combine(outputDir, "Directory.Packages.props");
    if (!File.Exists(packagesProps))
    {
      Terminal.WriteErrorLine($"Missing {packagesProps}.".Red());
      return false;
    }

    string text = File.ReadAllText(packagesProps);
    if (!TryEvaluatePlatformPins(text, expectedVersion, out List<string> failures, out int pinCount))
    {
      if (pinCount == 0)
      {
        Terminal.WriteErrorLine(
          "Directory.Packages.props: zero platform PackageVersion pins matched — pattern/fragment drift?".Red());
      }
      else
      {
        Terminal.WriteErrorLine(
          $"Directory.Packages.props: platform pins must equal release version {expectedVersion}:".Red());
        foreach (string failure in failures)
          Terminal.WriteErrorLine($"  {failure}");
      }

      return false;
    }

    Terminal.WriteLine($"Platform CPM pins == {expectedVersion} ({pinCount} pins).");
    return true;
  }

  /// <summary>
  /// Scans monorepo template-shipped consumer content (including .cs) for continuous
  /// TimeWarp.Architecture.&lt;suffix&gt; literals that sourceName would rewrite. Suffix set is
  /// derived from msbuild/timewarp-platform-packages.props.
  /// </summary>
  public bool AssertNoUnsafePlatformNamespaceLiterals()
  {
    Terminal.WriteLine("Scanning monorepo template-shipped content for unsafe platform namespace literals...");

    if (!TryEnsureArchitectureSuffixes(out IReadOnlyList<string> suffixes))
      return false;

    Terminal.WriteLine(
      $"Derived platform-namespace scan suffixes from timewarp-platform-packages.props: {string.Join(", ", suffixes)}");

    string alternation = string.Join(
      "|",
      suffixes.Select(static suffix => System.Text.RegularExpressions.Regex.Escape(suffix)));

    var unsafeLiteral = new System.Text.RegularExpressions.Regex(
      $@"TimeWarp\.Architecture\.({alternation})\b",
      System.Text.RegularExpressions.RegexOptions.Compiled);

    List<string> hits = [];

    foreach (string relativeRoot in SourceNameLiteralScanRelativeRoots)
    {
      string root = Path.Combine(RepoRoot, relativeRoot);
      if (!Directory.Exists(root))
        continue;

      foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
      {
        if (TemplateSmokePaths.IsBinObjOrArtifacts(RepoRoot, file))
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

  /// <summary>
  /// sourceName rewrite of package IDs produces e.g. SmokeDefault.Analyzers — nonexistent.
  /// Scoped to MSBuild/JSON (not .cs). Also requires generated platform props composition file.
  /// </summary>
  public bool AssertPackageIdsNotRewritten(string appName, string outputDir)
  {
    IReadOnlyList<string> fragments = GetForbiddenRewrittenFragments();
    if (fragments.Count == 0)
      return false;

    string[] forbidden = fragments.Select(suffix => appName + suffix).ToArray();

    List<string> hits = [];
    foreach (string file in Directory.EnumerateFiles(outputDir, "*.*", SearchOption.AllDirectories))
    {
      if (TemplateSmokePaths.IsBinObjOrArtifacts(outputDir, file))
        continue;

      string extension = Path.GetExtension(file);
      if (!PackageIdRewriteScanExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        continue;

      string text = File.ReadAllText(file);
      string relative = Path.GetRelativePath(outputDir, file);
      foreach (string token in forbidden)
      {
        if (text.Contains(token, StringComparison.Ordinal))
          hits.Add($"{relative}: contains '{token}'");
      }
    }

    string platformProps = Path.Combine(outputDir, "msbuild", "timewarp-platform-packages.props");
    if (!File.Exists(platformProps))
    {
      Terminal.WriteErrorLine(
        $"Missing {platformProps} — platform package ID composition file not in template output.".Red());
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

  /// <summary>
  /// Collects rewritten continuous platform tokens under <paramref name="root"/> using the
  /// sourceName-literal extension set (includes .cs). Used by smoke package-mode belt check.
  /// </summary>
  public List<string> CollectRewrittenPlatformTokenHits(string appName, string root)
  {
    IReadOnlyList<string> fragments = GetForbiddenRewrittenFragments();
    if (fragments.Count == 0)
      return ["(suffix derivation failed)"];

    string[] rewrittenTokens = fragments.Select(suffix => appName + suffix).ToArray();
    List<string> hits = [];

    foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
    {
      if (TemplateSmokePaths.IsBinObjOrArtifacts(root, file))
        continue;

      if (!IsSourceNameLiteralScanExtension(file))
        continue;

      string text = File.ReadAllText(file);
      string relative = Path.GetRelativePath(root, file);
      foreach (string token in rewrittenTokens)
      {
        if (text.Contains(token, StringComparison.Ordinal))
          hits.Add($"{relative}: contains rewritten '{token}'");
      }
    }

    return hits;
  }

  /// <summary>
  /// Co-located Jaribu runfiles (task 135) whose JARIBU_MULTI template-safety (the cnd:noEmit
  /// escape) and end-to-end runnability must survive <c>dotnet new</c> generation. These compile
  /// into NO layer project (registered-unrouted "tests" suffix, feature-filename-grammar.json) so
  /// the generated app's ordinary solution build is structurally blind to a regression here.
  /// Tiers 1–2 exercise standalone <c>dotnet run</c>; tier 3 (task 136) exercises the family
  /// <c>JARIBU_MULTI</c> aggregators via bare <c>dotnet test</c> from each project directory.
  /// </summary>
  public static readonly string[] CoLocatedTestFiles =
  [
    "source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs",
    "source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs",
  ];

  /// <summary>
  /// Per-family JARIBU_MULTI aggregator projects (task 136). Relative to the generated app root.
  /// ExpectedSucceeded matches co-located exemplar counts (web create-role = 5, api weather = 2).
  /// Serial execution required — api aggregator binds fixed port 7255.
  /// </summary>
  public static readonly (string RelativeProjectDir, int ExpectedSucceeded)[] JaribuFamilyAggregators =
  [
    ("tests/container-apps/web/web-jaribu-tests", 5),
    ("tests/container-apps/api/api-jaribu-tests", 2),
  ];

  // The exact guarded lines a template-safe co-located runfile preamble must contain, verbatim,
  // in the GENERATED output. dotnet-new's conditional processor (without the cnd:noEmit escape)
  // strips the "#if !JARIBU_MULTI" / "#endif" directive lines while keeping the body unconditional
  // (task 134 finding M1, reproduced and confirmed fixed by the escape during task 135) — so their
  // literal presence post-generation is the actual proof, not TWA0008 staying quiet (TWA0008 only
  // flags tokens OUTSIDE the escaped region).
  private static readonly string[] JaribuMultiGuardLines =
  [
    "#if !JARIBU_MULTI",
    "return await TimeWarp.Jaribu.TestRunner.RunAllTests();",
    "#endif",
  ];

  private static readonly System.Text.RegularExpressions.Regex AnsiEscape =
    new(@"\x1B\[[0-9;]*m", System.Text.RegularExpressions.RegexOptions.Compiled);

  private static readonly System.Text.RegularExpressions.Regex JaribuTotalLine =
    new(@"Total:\s*(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

  private static readonly System.Text.RegularExpressions.Regex JaribuPassedLine =
    new(@"Passed:\s*(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

  // MTP `dotnet test` summary lines (case-insensitive): "total: N" / "succeeded: N"
  private static readonly System.Text.RegularExpressions.Regex MtpTotalLine =
    new(@"^\s*total:\s*(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  private static readonly System.Text.RegularExpressions.Regex MtpSucceededLine =
    new(@"^\s*succeeded:\s*(\d+)\s*$", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

  /// <summary>
  /// Tier 1 (cheap): asserts every co-located test file that shipped into the generated app
  /// still contains the JARIBU_MULTI guard lines verbatim. Run right after generation, before
  /// restore/build, so a regression here is reported before spending time on a build.
  /// </summary>
  public bool AssertCoLocatedTestFilesSurviveGeneration(string outputDir)
  {
    bool ok = true;

    foreach (string relativePath in CoLocatedTestFiles)
    {
      string generatedPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
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
  /// Tier 2 (authoritative): `dotnet run` each generated co-located runfile standalone and assert
  /// exit 0 with a nonzero, all-passing Jaribu grand total. Proves the file is not just textually
  /// intact but genuinely compiles and runs end to end post-generation (sourceName namespace
  /// rewrite, contract shape, everything). Runs files serially — the weather-forecast file spins
  /// a real host on fixed port 7255 (same constraint as the monorepo original).
  /// </summary>
  public async Task<bool> AssertCoLocatedTestFilesRunAsync(string outputDir, CancellationToken ct)
  {
    bool ok = true;

    foreach (string relativePath in CoLocatedTestFiles)
    {
      string generatedPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
      if (!File.Exists(generatedPath))
        continue; // already reported by tier 1

      Terminal.WriteLine($"Running generated co-located test standalone: {relativePath}...");

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("run", generatedPath)
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .CaptureAsync(ct);

      string plain = AnsiEscape.Replace(result.Combined, "");
      bool parsed = TryParseJaribuGrandTotal(plain, out int total, out int passed);

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
  /// Parses the LAST "Total: N" / "Passed: N" pair in Jaribu TerminalSink output — the Grand
  /// Total across every test class in the file, not a per-class subtotal.
  /// </summary>
  private static bool TryParseJaribuGrandTotal(string plainOutput, out int total, out int passed)
  {
    total = 0;
    passed = 0;

    System.Text.RegularExpressions.MatchCollection totalMatches = JaribuTotalLine.Matches(plainOutput);
    System.Text.RegularExpressions.MatchCollection passedMatches = JaribuPassedLine.Matches(plainOutput);
    if (totalMatches.Count == 0 || passedMatches.Count == 0)
      return false;

    total = int.Parse(totalMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    passed = int.Parse(passedMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    return true;
  }

  /// <summary>
  /// Tier 3 (task 136): assert each family JARIBU_MULTI aggregator exists in the generated app
  /// and that bare <c>dotnet test -c Release</c> from that project directory reports the expected
  /// succeeded count. Aggregators are not in .slnx (solution build never compiles them); this is
  /// the multi-mode / MTP regression gate. Serial — api aggregator uses fixed port 7255.
  /// </summary>
  public async Task<bool> AssertJaribuFamilyAggregatorsAsync(string outputDir, CancellationToken ct)
  {
    bool ok = true;

    foreach ((string relativeProjectDir, int expectedSucceeded) in JaribuFamilyAggregators)
    {
      string projectDir = Path.Combine(outputDir, relativeProjectDir.Replace('/', Path.DirectorySeparatorChar));
      string csprojName = Path.GetFileName(relativeProjectDir) + ".csproj";
      string csprojPath = Path.Combine(projectDir, csprojName);

      if (!Directory.Exists(projectDir) || !File.Exists(csprojPath))
      {
        Terminal.WriteErrorLine(
          $"Jaribu family aggregator missing after generation: {relativeProjectDir}/{csprojName}".Red());
        ok = false;
        continue;
      }

      Terminal.WriteLine(
        $"Running generated Jaribu aggregator (MTP bare dotnet test): {relativeProjectDir} (expect {expectedSucceeded} succeeded)...");

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("test", "-c", "Release")
        .WithWorkingDirectory(projectDir)
        .WithNoValidation()
        .CaptureAsync(ct);

      string plain = AnsiEscape.Replace(result.Combined, "");
      bool parsed = TryParseMtpSummary(plain, out int total, out int succeeded);

      if (!result.Success || !parsed || total != expectedSucceeded || succeeded != expectedSucceeded)
      {
        Terminal.WriteErrorLine(
          $"{relativeProjectDir}: aggregator did not report {expectedSucceeded}/{expectedSucceeded} (exit {result.ExitCode}; parsed total={total}, succeeded={succeeded}).".Red());
        Terminal.WriteErrorLine(result.Combined);
        ok = false;
        continue;
      }

      Terminal.WriteLine($"{relativeProjectDir}: {succeeded}/{total} succeeded via MTP.".Green());
    }

    return ok;
  }

  /// <summary>
  /// Parses Microsoft.Testing.Platform <c>dotnet test</c> summary lines
  /// (<c>total: N</c> / <c>succeeded: N</c>). Uses the last match of each.
  /// </summary>
  private static bool TryParseMtpSummary(string plainOutput, out int total, out int succeeded)
  {
    total = 0;
    succeeded = 0;

    System.Text.RegularExpressions.MatchCollection totalMatches = MtpTotalLine.Matches(plainOutput);
    System.Text.RegularExpressions.MatchCollection succeededMatches = MtpSucceededLine.Matches(plainOutput);
    if (totalMatches.Count == 0 || succeededMatches.Count == 0)
      return false;

    total = int.Parse(totalMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    succeeded = int.Parse(succeededMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    return true;
  }

  /// <summary>
  /// Shared generate → package-id assert → optional prepare → find solution → restore → build.
  /// </summary>
  /// <param name="restoreNarration">
  /// Optional label after "Restoring {solution}"; e.g. " (local TimeWarp.* feed)" or
  /// " (nuget.org only)". Null → no suffix.
  /// </param>
  /// <param name="environment">
  /// Optional process env for generate / restore / build (publish isolation hive).
  /// </param>
  public async Task<bool> SmokeOneAsync(
    string name,
    string workDir,
    string[] extraArgs,
    string templateShortName,
    CancellationToken ct,
    Func<string, Task<bool>>? afterGenerateAsync = null,
    string? restoreNarration = null,
    IReadOnlyDictionary<string, string>? environment = null)
  {
    string outputDir = Path.Combine(workDir, name);
    Terminal.WriteLine($"\n── Generate {name} ──".Cyan());

    List<string> args =
    [
      "new", templateShortName,
      "-n", name,
      "-o", outputDir,
      "--force",
    ];
    args.AddRange(extraArgs);

    ShellBuilder generateBuilder = Shell.Builder("dotnet")
      .WithArguments([.. args])
      .WithWorkingDirectory(RepoRoot)
      .WithNoValidation();
    generateBuilder = ApplyShellEnvironment(generateBuilder, environment);

    CommandOutput generate = await generateBuilder.CaptureAsync(ct);

    if (!generate.Success)
    {
      Terminal.WriteErrorLine(generate.Combined);
      Terminal.WriteErrorLine($"dotnet new failed for {name}!".Red());
      return false;
    }

    Terminal.WriteLine(generate.Combined);

    if (!AssertPackageIdsNotRewritten(name, outputDir))
      return false;

    if (afterGenerateAsync is not null && !await afterGenerateAsync(outputDir))
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

    Terminal.WriteLine($"Restoring {solution}{restoreNarration}...");

    DotNetRestoreBuilder restoreBuilder = DotNet.Restore()
      .WithProject(solution)
      .WithWorkingDirectory(outputDir)
      .WithNoValidation();
    restoreBuilder = ApplyCommandEnvironment(restoreBuilder, environment);

    int restoreExit = await restoreBuilder.RunAsync(ct);
    if (restoreExit != 0)
    {
      Terminal.WriteErrorLine($"Restore failed for {name}!".Red());
      return false;
    }

    Terminal.WriteLine($"Building {solution} (Release)...");
    DotNetBuildBuilder buildBuilder = DotNet.Build()
      .WithProject(solution)
      .WithConfiguration("Release")
      .WithNoRestore()
      .WithWorkingDirectory(outputDir)
      .WithNoValidation();
    buildBuilder = ApplyCommandEnvironment(buildBuilder, environment);

    int buildExit = await buildBuilder.RunAsync(ct);
    if (buildExit != 0)
    {
      Terminal.WriteErrorLine($"Build failed for {name}!".Red());
      return false;
    }

    Terminal.WriteLine($"{name} OK".Green());
    return true;
  }

  private static ShellBuilder ApplyShellEnvironment(
    ShellBuilder builder,
    IReadOnlyDictionary<string, string>? environment)
  {
    if (environment is null)
      return builder;

    foreach ((string key, string value) in environment)
      builder = builder.WithEnvironmentVariable(key, value);

    return builder;
  }

  private static TBuilder ApplyCommandEnvironment<TBuilder>(
    TBuilder builder,
    IReadOnlyDictionary<string, string>? environment)
    where TBuilder : ICommandBuilder<TBuilder>
  {
    if (environment is null)
      return builder;

    foreach ((string key, string value) in environment)
      builder = builder.WithEnvironmentVariable(key, value);

    return builder;
  }
}
