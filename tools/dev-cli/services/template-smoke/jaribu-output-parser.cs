#region Purpose
// Parses generated-app test output: strips ANSI and reads the Jaribu grand total.
#endregion
#region Design
// MTP total:/succeeded:/failed:/skipped: regexes live on JaribuAggregatorSummaryGate
// (compile-included by tests). The Total:/Passed: lines parsed here are Jaribu TerminalSink output
// from tier 2 (standalone runfile), where the LAST pair is the grand total.
#endregion

namespace DevCli.Services;

/// <summary>
/// Output parsing for Jaribu runfile and MTP runs of generated apps.
/// </summary>
internal static partial class JaribuOutputParser
{
  [GeneratedRegex(@"\x1B\[[0-9;]*m")]
  private static partial Regex AnsiEscape();

  [GeneratedRegex(@"Total:\s*(\d+)")]
  private static partial Regex JaribuTotalLine();

  [GeneratedRegex(@"Passed:\s*(\d+)")]
  private static partial Regex JaribuPassedLine();

  /// <summary>Removes ANSI color escape sequences from captured process output.</summary>
  public static string StripAnsi(string text) => AnsiEscape().Replace(text, "");

  /// <summary>
  /// Parses the LAST "Total: N" / "Passed: N" pair in Jaribu TerminalSink output — the Grand
  /// Total across every test class in the file, not a per-class subtotal.
  /// </summary>
  public static bool TryParseJaribuGrandTotal(string plainOutput, out int total, out int passed)
  {
    total = 0;
    passed = 0;

    System.Text.RegularExpressions.MatchCollection totalMatches = JaribuTotalLine().Matches(plainOutput);
    System.Text.RegularExpressions.MatchCollection passedMatches = JaribuPassedLine().Matches(plainOutput);
    if (totalMatches.Count == 0 || passedMatches.Count == 0)
      return false;

    total = int.Parse(totalMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    passed = int.Parse(passedMatches[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    return true;
  }
}
