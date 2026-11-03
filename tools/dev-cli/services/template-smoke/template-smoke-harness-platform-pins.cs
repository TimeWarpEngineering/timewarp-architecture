#region Purpose
// TemplateSmokeHarness partial: platform CPM pin rewrite/equality and sourceName rewrite asserts.
#endregion
#region Design
// Task 115 / 131-003: template sourceName is TimeWarp.Architecture, so package-id literals
// TimeWarp.Architecture.* would rewrite to SmokeDefault.* and break restore. The monorepo scan finds
// unsafe literals before packing; the generated-app scans find rewritten ids afterwards. Suffix sets
// come from the props SSOT; extension and root tables from SourceNameLiteralScanSets.
#endregion

namespace DevCli.Services;

using static DevCli.Services.SourceNameLiteralScanSets;

internal sealed partial class TemplateSmokeHarness
{
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

    text = PlatformPinRules.RewritePins(text, targetVersion);

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
    if (!PlatformPinRules.TryEvaluatePlatformPins(text, expectedVersion, out List<string> failures, out int pinCount))
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
}
