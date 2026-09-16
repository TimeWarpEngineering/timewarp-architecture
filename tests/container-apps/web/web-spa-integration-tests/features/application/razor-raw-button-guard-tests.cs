#region Purpose
// Fail if any .razor under web-spa/features contains a raw <button — actions are FluentButton.
#endregion

#region Design
// Task 233: Settings was the last features page with native buttons. A source scan is cheaper
// than prerender HTML and catches the regression before a host boots. Allow-list is only for
// shared internals under components/ (empty: features must not wrap a native button).
#endregion

namespace RazorRawButtonGuard_;

[TestTag("Unit")]
public class FeaturesRazor_Should_
{
  // Allow-list is reserved for a features-local wrapper that must emit a native button.
  // Shared elements live under web-spa/components/ and are outside this scan.
  private static readonly string[] AllowListedRelativePaths = [];

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeaturesRazor_Should_>();

  public static Task Not_Contain_Raw_Button_Elements()
  {
    string repoRoot = FindRepoRoot();
    string featuresDirectory = Path.Combine(
      repoRoot,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features");
    Directory.Exists(featuresDirectory).ShouldBeTrue(featuresDirectory);

    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(featuresDirectory, "*.razor", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(featuresDirectory, file);
      if (IsAllowListed(relative))
      {
        continue;
      }

      string source = File.ReadAllText(file);
      if (source.Contains("<button", StringComparison.OrdinalIgnoreCase))
      {
        offenders.Add(relative);
      }
    }

    offenders.ShouldBeEmpty(
      "Raw <button> is banned under web-spa/features. Use FluentButton (see StyleGuidePage). Offenders: "
      + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

  private static bool IsAllowListed(string relativePath) =>
    AllowListedRelativePaths.Contains(relativePath, StringComparer.OrdinalIgnoreCase);

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}
