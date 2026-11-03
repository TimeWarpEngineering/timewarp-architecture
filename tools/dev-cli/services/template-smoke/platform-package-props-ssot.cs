#region Purpose
// Derives the Architecture.* first-segment suffixes from msbuild/timewarp-platform-packages.props.
#endregion
#region Design
// Task 131-003 / F-007: all rewrite-scan suffixes and nupkg-exclude fragments derive from
// msbuild/timewarp-platform-packages.props (126-006 style); there are no hand ForbiddenRewritten arrays.
// The minimal suffix set (Analyzers/Attributes/Generators/TypedIds) is fail-closed so props drift
// cannot silently narrow the gates.
#endregion

namespace DevCli.Services;

/// <summary>
/// Derives Architecture.* first-segment suffixes from timewarp-platform-packages.props SSOT.
/// </summary>
internal static partial class PlatformPackagePropsSsot
{
  /// <summary>
  /// Extracts the first identifier after <c>.Architecture.</c> from composed props values such as
  /// <c>$(_TwPlatformVendor).Architecture.Analyzers</c> or continuous
  /// <c>TimeWarp.Architecture.TypedIds.Ef</c>.
  /// </summary>
  [GeneratedRegex(@"(?:\$\(_TwPlatformVendor\)|TimeWarp)\.Architecture\.([A-Za-z_][A-Za-z0-9_]*)")]
  public static partial Regex ComposedArchitectureSuffix();

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

        System.Text.RegularExpressions.Match match = ComposedArchitectureSuffix().Match(value);
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
