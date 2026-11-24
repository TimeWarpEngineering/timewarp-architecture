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
