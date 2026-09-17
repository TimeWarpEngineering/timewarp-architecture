#region Purpose
// Fail if any .razor under web-spa/features contains the hand-written Loading… / Loading... literal.
#endregion

#region Design
// Task 236: loading UI is Section (FluentSpinner) or FluentDataGrid Loading, never a
// hand-written "Loading…" paragraph. Source scan matches the raw-button guard.
#endregion

namespace RazorLoadingLiteralGuard_;

[TestTag("Unit")]
public class FeaturesRazor_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeaturesRazor_Should_>();

  public static Task Not_Contain_Hand_Written_Loading_Literal()
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
      string source = File.ReadAllText(file);
      if (source.Contains("Loading…", StringComparison.Ordinal)
        || source.Contains("Loading...", StringComparison.Ordinal))
      {
        offenders.Add(relative);
      }
    }

    offenders.ShouldBeEmpty(
      "Hand-written Loading… is banned under web-spa/features. Use Section (see StyleGuidePage). Offenders: "
      + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

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
