#region Purpose
// Template smoke: the generated app ships its CI workflow and the setup-ci action, and the workflow
// has the app's shape.
#endregion
#region Design
// Task 531. Runs right after generation, in every variant. The app's .github/ holds exactly
// .github/workflows/workflow.yml and .github/actions/setup-ci/action.yml: the template repository's
// own workflows and the .github/app-workflows/ source path must not ship. setup-ci is one file used
// by both repositories, so the app's copy must equal this repository's byte for byte. The workflow
// must equal its source (.github/app-workflows/workflow.yml): it has no flag regions, so every
// variant gets the same file, and a template token that generation rewrote inside it fails here.
// AppCiWorkflowShape.Check then parses it and checks the app's jobs and phases.
#endregion

namespace DevCli.Services;

/// <summary>Shipped-file and shape checks for the generated app's CI workflow.</summary>
internal sealed class TemplateSmokeCiWorkflow
{
  private const string GitHubDirectory = ".github";

  private readonly ITerminal Terminal;
  private readonly string RepoRoot;

  public TemplateSmokeCiWorkflow(ITerminal terminal, string repoRoot)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
  }

  /// <summary>True when the generated app's .github/ is the app workflow and setup-ci, and the workflow passes the shape check.</summary>
  public bool AssertCiWorkflowShipped(string outputDir)
  {
    string[] expected = [AppCiWorkflowShape.AppRelativePath, AspireVersionSources.SetupActionRelativePath];
    string gitHub = Path.Combine(outputDir, GitHubDirectory);
    string[] shipped = Directory.Exists(gitHub)
      ? [.. Directory.EnumerateFiles(gitHub, "*", SearchOption.AllDirectories)
          .Select(file => Path.GetRelativePath(outputDir, file).Replace('\\', '/'))
          .Order(StringComparer.Ordinal)]
      : [];

    bool ok = true;
    foreach (string missing in expected.Where(path => !shipped.Contains(path, StringComparer.Ordinal)))
    {
      Terminal.WriteErrorLine($"Generated app is missing {missing}: the template pack must ship the app's CI (task 531).".Red());
      ok = false;
    }

    foreach (string extra in shipped.Where(path => !expected.Contains(path, StringComparer.Ordinal)))
    {
      Terminal.WriteErrorLine($"Generated app contains {extra}: only the app workflow and setup-ci ship under .github/.".Red());
      ok = false;
    }

    if (!ok) return false;

    ok &= SameAsSource(outputDir, AspireVersionSources.SetupActionRelativePath, AspireVersionSources.SetupActionRelativePath);
    ok &= SameAsSource(outputDir, AppCiWorkflowShape.AppRelativePath, AppCiWorkflowShape.SourceRelativePath);

    string workflow = File.ReadAllText(Path.Combine(outputDir, AppCiWorkflowShape.AppRelativePath));
    IReadOnlyList<string> errors = AppCiWorkflowShape.Check(workflow);
    foreach (string error in errors)
    {
      Terminal.WriteErrorLine($"Generated {AppCiWorkflowShape.AppRelativePath}: {error}".Red());
    }

    if (!ok || errors.Count > 0) return false;

    Terminal.WriteLine("Generated app ships its CI workflow and setup-ci.");
    return true;
  }

  private bool SameAsSource(string outputDir, string generatedRelative, string sourceRelative)
  {
    string generated = File.ReadAllText(Path.Combine(outputDir, generatedRelative));
    string source = File.ReadAllText(Path.Combine(RepoRoot, sourceRelative));
    if (string.Equals(generated, source, StringComparison.Ordinal)) return true;

    Terminal.WriteErrorLine($"Generated {generatedRelative} differs from {sourceRelative}: generation must copy it unchanged.".Red());
    return false;
  }
}
