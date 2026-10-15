#region Purpose
// The AppHost publish targets `dev publish` knows, and the pure `dotnet test` arguments that run their
// aspire-tests production-safety suites.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Each target names its Publish:Target, its output directory, its safety-suite class and the
// variable that points that suite at the CLI output (tasks 070-003 / 070-004 / 070-007).
// Task 367: `dev publish all` checks all three outputs in ONE aspire-tests run instead of three.
// Jaribu's --filter-class takes exactly one substring of the class FullName, so the combined run
// filters on SafetySuiteClassSuffix, which every target's class must end with (dev-cli-tests pin
// it). A single target keeps its own class filter. --no-build is for a caller that just ran the
// Release solution build (`dev workflow`); aspire-tests is listed in the .slnx, so the build
// produced its Release output and the run skips build and restore.
#endregion

namespace DevCli.Services;

/// <summary>One AppHost publish target and the aspire-tests class that guards its output.</summary>
internal sealed record AspirePublishTarget(
  string Name,
  string OutputDirectory,
  string SafetySuiteClass,
  string OutputEnvironmentVariable);

/// <summary>The publish targets and the <c>dotnet test</c> arguments for their safety suites.</summary>
internal static class AspirePublishTargets
{
  internal const string SafetySuiteClassSuffix = "Publish_Given_";

  internal static readonly AspirePublishTarget Compose = new(
    Name: "compose",
    OutputDirectory: "artifacts/aspire-output/compose",
    SafetySuiteClass: "ComposePublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_COMPOSE_OUTPUT");

  internal static readonly AspirePublishTarget Kubernetes = new(
    Name: "kubernetes",
    OutputDirectory: "artifacts/aspire-output/kubernetes",
    SafetySuiteClass: "KubernetesPublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_HELM_OUTPUT");

  internal static readonly AspirePublishTarget Aca = new(
    Name: "aca",
    OutputDirectory: "artifacts/aspire-output/aca",
    SafetySuiteClass: "AcaPublish_Given_",
    OutputEnvironmentVariable: "TIMEWARP_ACA_OUTPUT");

  /// <summary>Every target, in the order `dev publish all` publishes them.</summary>
  internal static readonly AspirePublishTarget[] All = [Compose, Kubernetes, Aca];

  /// <summary>The <c>dotnet test</c> arguments that run the safety suites of <paramref name="targets"/> in one invocation.</summary>
  internal static string[] SafetySuiteTestArguments(IReadOnlyList<AspirePublishTarget> targets, bool noBuild)
  {
    string filter = targets.Count == 1 ? targets[0].SafetySuiteClass : SafetySuiteClassSuffix;
    string[] build = noBuild ? ["--no-build", "--no-restore"] : [];
    return ["test", "-c", "Release", .. build, "--", "--filter-class", filter];
  }
}
