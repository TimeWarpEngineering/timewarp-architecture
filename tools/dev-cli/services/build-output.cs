#region Purpose
// Output rules for `dev build`: whether MSBuild output is captured or streamed, and the one-line
// summary a captured build prints when it succeeds.
#endregion

#region Design
// Capture is the default so a green build prints a few lines and agents have no reason to pipe
// `dev build` through tail or head (that truncates diagnostics and hides the exit code). A captured
// build that fails prints the whole MSBuild output, so no diagnostic is lost. --verbose streams
// MSBuild live. --quiet is kept so older callers still parse; it names the default. When both are
// passed, --verbose wins: it is the flag that asks for something the default does not do.
// DevCliRunfile is the dev CLI entry point `dev build` compiles last (task 535). It is a .NET
// file-based app, not in the .slnx, so without that step a tools/dev-cli/ compile error passed
// `dev build` and showed up only at `dev self-install`. Template smoke reads the same constant.
// Pure rules (no Amuru, no terminal), so dev-cli-tests Compile-include this file.
#endregion

namespace DevCli.Services;

using System.Globalization;

/// <summary>How <c>dev build</c> handles MSBuild output.</summary>
internal enum BuildOutputMode
{
  /// <summary>Capture MSBuild output; print it only when the step fails.</summary>
  Capture,

  /// <summary>Stream MSBuild output to the terminal as it runs.</summary>
  Stream,
}

/// <summary>Output rules for <c>dev build</c>.</summary>
internal static class BuildOutput
{
  internal const string SuccessText = "Build completed successfully!";

  /// <summary>The dev CLI runfile, relative to the repository root. It is not in the solution.</summary>
  internal const string DevCliRunfile = "tools/dev-cli/dev.cs";

  /// <summary>The step line <c>dev build</c> prints before it compiles the dev CLI runfile.</summary>
  internal const string DevCliStepText = $"Building {DevCliRunfile} (dev CLI runfile, Release, no NativeAOT link)...";

  /// <summary>Picks the output mode from the <c>--verbose</c> and <c>--quiet</c> flags.</summary>
  internal static BuildOutputMode Select(bool verbose, bool quiet)
  {
    _ = quiet; // --quiet names the default; kept so existing callers still parse.
    return verbose ? BuildOutputMode.Stream : BuildOutputMode.Capture;
  }

  /// <summary>The line a successful build prints: elapsed seconds, repository root, success text.</summary>
  internal static string SuccessSummary(string repoRoot, TimeSpan elapsed)
  {
    string seconds = elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture);
    return $"{SuccessText} ({seconds}s, {repoRoot})";
  }
}
