#region Purpose
// Host-free template-smoke gate: generated SPA initializer static `_content` imports exist in the NuGet cache.
#endregion

#region Design
// Task 240: a PascalCase `_content` specifier 404s the whole Blazor JS initializer. The runtime
// HTTP smoke lives in web-server-integration-tests; this check runs after each generated app
// build with no server. Root-relative `/_content/<PackageId>/...` maps to
// `{NuGet packages}/<package-id-lower>/<version>/staticwebassets/...` using Directory.Packages.props
// versions. Recurses one level. Relative specifiers are skipped (same rule as the HTTP smoke).
#endregion

namespace DevCli.Services;

internal sealed partial class TemplateSmokeHarness
{
  [GeneratedRegex(@"^import\s+.+?\s+from\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex ImportFromSpecifier();

  [GeneratedRegex(@"^import\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex SideEffectImportSpecifier();

  [GeneratedRegex(@"from\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex FromSpecifier();

  /// <summary>
  /// After a generated-app build, parse <c>web.spa.lib.module.js</c> static imports and assert
  /// each root-relative <c>_content</c> specifier exists as a file in the NuGet cache.
  /// </summary>
  public bool AssertInitializerImportGraphResolves(string outputDir)
  {
    string webFamilyDir = Path.Combine(outputDir, "source", "container-apps", "web");
    if (!Directory.Exists(webFamilyDir))
    {
      Terminal.WriteLine("Skipping initializer import-graph check (web family absent).");
      return true;
    }

    string emitPath = Path.Combine(
      webFamilyDir,
      "projects",
      "web-spa",
      "wwwroot",
      "js",
      "web.spa.lib.module.js");
    if (!File.Exists(emitPath))
    {
      Terminal.WriteErrorLine(
        $"Generated web-spa initializer emit missing at {emitPath}. The solution build must run tsc.".Red());
      return false;
    }

    Dictionary<string, string> packageVersions = LoadPackageVersions(outputDir);
    if (packageVersions.Count == 0)
    {
      Terminal.WriteErrorLine(
        $"No PackageVersion entries under {outputDir}. Cannot resolve _content imports.".Red());
      return false;
    }

    string packagesRoot = ResolveNuGetPackagesRoot();
    HashSet<string> checkedFiles = new(StringComparer.OrdinalIgnoreCase);
    List<string> missing = [];
    int contentImports = 0;

    Queue<string> pending = new();
    pending.Enqueue(emitPath);

    while (pending.Count > 0)
    {
      string file = pending.Dequeue();
      if (!checkedFiles.Add(file))
        continue;

      if (!File.Exists(file))
      {
        missing.Add(file);
        continue;
      }

      bool isEmitRoot = string.Equals(file, emitPath, StringComparison.OrdinalIgnoreCase);
      foreach (string specifier in ParseStaticImportSpecifiers(File.ReadAllText(file)))
      {
        if (!TryResolveContentSpecifier(specifier, packageVersions, packagesRoot, out string? resolved, out string? error))
        {
          if (error is not null)
          {
            missing.Add(error);
          }

          continue;
        }

        contentImports++;
        if (isEmitRoot && resolved is not null)
        {
          pending.Enqueue(resolved);
        }
        else if (resolved is not null && !File.Exists(resolved))
        {
          missing.Add($"{specifier} → {resolved}");
        }
      }
    }

    if (contentImports == 0)
    {
      Terminal.WriteErrorLine(
        $"{emitPath} has no root-relative _content imports. The SPA initializer must import TimeWarp.State / State.Plus assets.".Red());
      return false;
    }

    if (missing.Count > 0)
    {
      Terminal.WriteErrorLine("Initializer import graph is missing NuGet static web assets:".Red());
      foreach (string item in missing.Distinct(StringComparer.OrdinalIgnoreCase))
        Terminal.WriteErrorLine($"  {item}".Red());
      return false;
    }

    Terminal.WriteLine(
      $"Initializer import graph OK ({contentImports} _content specifier(s) resolved under {packagesRoot}).");
    return true;
  }

  private static string ResolveNuGetPackagesRoot()
  {
    string? nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
    if (!string.IsNullOrWhiteSpace(nugetPackages))
      return nugetPackages;

    return Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuget",
      "packages");
  }

  private static Dictionary<string, string> LoadPackageVersions(string outputDir)
  {
    Dictionary<string, string> versions = new(StringComparer.OrdinalIgnoreCase);
    foreach (string props in Directory.EnumerateFiles(outputDir, "Directory.Packages.props", SearchOption.AllDirectories))
    {
      if (TemplateSmokePaths.IsBinObjOrArtifacts(outputDir, props))
        continue;

      var document = XDocument.Load(props);
      foreach (XElement element in document.Descendants("PackageVersion"))
      {
        string? packageId = (string?)element.Attribute("Include");
        string? version = (string?)element.Attribute("Version");
        if (!string.IsNullOrWhiteSpace(packageId) && !string.IsNullOrWhiteSpace(version))
          versions[packageId] = version;
      }
    }

    return versions;
  }

  private static IReadOnlyList<string> ParseStaticImportSpecifiers(string source)
  {
    HashSet<string> specifiers = new(StringComparer.Ordinal);
    AddMatches(specifiers, ImportFromSpecifier(), source);
    AddMatches(specifiers, SideEffectImportSpecifier(), source);
    AddMatches(specifiers, FromSpecifier(), source);
    return [.. specifiers];
  }

  private static void AddMatches(HashSet<string> specifiers, Regex regex, string source)
  {
    foreach (Match match in regex.Matches(source))
    {
      if (match.Success && match.Groups.Count > 1 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
        specifiers.Add(match.Groups[1].Value);
    }
  }

  private static bool TryResolveContentSpecifier(
    string specifier,
    Dictionary<string, string> packageVersions,
    string packagesRoot,
    out string? resolvedPath,
    out string? error)
  {
    resolvedPath = null;
    error = null;

    if (string.IsNullOrWhiteSpace(specifier) || !specifier.StartsWith("/_content/", StringComparison.Ordinal))
      return false;

    string relative = specifier["/_content/".Length..];
    int slash = relative.IndexOf('/');
    if (slash <= 0 || slash == relative.Length - 1)
    {
      error = $"Malformed _content specifier: {specifier}";
      return false;
    }

    string packageId = relative[..slash];
    string assetPath = relative[(slash + 1)..].Replace('/', Path.DirectorySeparatorChar);

    if (!packageVersions.TryGetValue(packageId, out string? version))
    {
      error = $"{specifier} (PackageVersion for {packageId} not found)";
      return false;
    }

    resolvedPath = Path.Combine(
      packagesRoot,
      packageId.ToLowerInvariant(),
      version,
      "staticwebassets",
      assetPath);

    if (!File.Exists(resolvedPath))
    {
      error = $"{specifier} → {resolvedPath}";
      return false;
    }

    return true;
  }
}
