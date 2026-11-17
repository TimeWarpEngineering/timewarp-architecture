#region Purpose
// Platform CPM pin matching: which PackageVersion includes are platform pins, equality evaluation, rewrite.
#endregion
#region Design
// Task 131-003: platform CPM pin matching uses one PlatformPinIncludeFragments list (TwArchitecture* +
// Foundation/Modules/Identity). It is a hand list by design, not a full props derivation. Static and
// state-free so template-smoke, template-publish-smoke and tests share the same matcher.
#endregion

namespace DevCli.Services;

/// <summary>
/// Platform PackageVersion pin matching shared by template-smoke and template-publish-smoke.
/// </summary>
internal static partial class PlatformPinRules
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
    "TimeWarp.402",
  ];

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
      PackageVersionIncludeVersion().Matches(packagesPropsText);

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
  /// Rewrites every platform PackageVersion pin in <paramref name="packagesPropsText"/> to
  /// <paramref name="targetVersion"/>; non-platform pins are returned unchanged.
  /// </summary>
  public static string RewritePins(string packagesPropsText, string targetVersion) =>
    PackageVersionIncludeAnyVersion().Replace(
      packagesPropsText,
      match =>
      {
        string include = match.Groups[1].Value;
        if (!IsPlatformPinInclude(include))
          return match.Value;
        return $"PackageVersion Include=\"{include}\" Version=\"{targetVersion}\"";
      });

  [GeneratedRegex(@"PackageVersion\s+Include=""([^""]+)""\s+Version=""([^""]+)""")]
  private static partial Regex PackageVersionIncludeVersion();

  [GeneratedRegex(@"PackageVersion\s+Include=""([^""]+)""\s+Version=""[^""]+""")]
  private static partial Regex PackageVersionIncludeAnyVersion();
}
