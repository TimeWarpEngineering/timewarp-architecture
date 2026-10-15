#region Purpose
// The one definition of the `dev test --group` test groups that CI runs as parallel jobs.
#endregion

#region Design
// Groups are derived by convention from the repo-relative project path, so workflow.yml names a
// group and never lists projects (task 366):
//   fast        — everything outside tests/container-apps/, plus container-app *-contracts-tests
//                 and *-domain-tests (pure, no host): analyzers, sourcegen, foundation, libraries,
//                 common, tools.
//   playwright  — *-playwright-tests (browser proof; CI uploads its screenshots).
//   integration — the rest of tests/container-apps/: *-integration-tests, aspire-tests,
//                 *-infrastructure-tests (Testcontainers), *-jaribu-tests aggregators.
// Every test project must match exactly one group. `dev test --group` refuses to run when one does
// not, and dev-cli-tests guards the monorepo's tests/ tree, so a new project cannot silently fall
// out of CI. Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

internal static class TestGroups
{
  internal const string Fast = "fast";
  internal const string Playwright = "playwright";
  internal const string Integration = "integration";

  internal static readonly string[] Names = [Fast, Playwright, Integration];

  private const string ContainerAppsFolder = "tests/container-apps/";
  private static readonly string[] PureSuffixes = ["-contracts-tests", "-domain-tests"];
  private const string PlaywrightSuffix = "-playwright-tests";

  internal static bool IsGroup(string name) => Names.Contains(name, StringComparer.Ordinal);

  /// <summary>
  /// Every group the project at <paramref name="relativeProjectPath"/> (repo-relative, '/' or '\'
  /// separators) belongs to. A well-formed tree yields exactly one.
  /// </summary>
  internal static string[] GroupsOf(string relativeProjectPath)
  {
    string path = relativeProjectPath.Replace('\\', '/');
    string name = Path.GetFileNameWithoutExtension(path);
    bool containerApp = path.StartsWith(ContainerAppsFolder, StringComparison.Ordinal);
    bool pure = PureSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal));
    bool playwright = name.EndsWith(PlaywrightSuffix, StringComparison.Ordinal);

    List<string> groups = [];
    if (!containerApp || pure) groups.Add(Fast);
    if (playwright) groups.Add(Playwright);
    if (containerApp && !pure && !playwright) groups.Add(Integration);
    return [.. groups];
  }
}
