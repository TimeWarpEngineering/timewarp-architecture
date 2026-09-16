#region Purpose
// Pure MTP aggregator-summary parse and pass/fail/warn decision for template-smoke tier 3.
#endregion

#region Design
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
// Gate is "the generated aggregator's suite ran and nothing failed", not an exact test count:
// exit 0 is a harness precondition; failed == 0 (failed: line required); succeeded at or above
// a floor; total == succeeded + skipped (skipped: missing means 0). Unparsable summary fails
// so a silent zero-discovery cannot pass. Succeeded above 2× the floor is a warning only —
// raise MinimumSucceeded deliberately so the floor stays a meaningful discovery floor.
// MTP host lines are lowercase (total:/succeeded:/failed:/skipped:); regexes are IgnoreCase
// and also accept the compact `Test summary: total: N, failed: …` form.
#endregion

namespace DevCli.Services;

using System.Globalization;
using System.Text.RegularExpressions;

internal readonly record struct MtpSummary(int Total, int Succeeded, int Failed, int Skipped);

internal enum AggregatorSummaryVerdict
{
  Pass,
  WarnStaleFloor,
  FailUnparsable,
  FailHasFailures,
  FailBelowFloor,
  FailTotalMismatch,
}

internal readonly record struct AggregatorSummaryDecision(
  AggregatorSummaryVerdict AggregatorSummaryVerdict,
  string Message)
{
  internal bool Passed =>
    AggregatorSummaryVerdict is AggregatorSummaryVerdict.Pass or AggregatorSummaryVerdict.WarnStaleFloor;
}

internal static partial class JaribuAggregatorSummaryGate
{
  // MTP `dotnet test` summary (case-insensitive). Prefer multi-line host lines
  // (`total: N` / `succeeded: N` / `failed: N` / `skipped: N` alone on a line) but also
  // accept the compact form `Test summary: total: N, failed: …, succeeded: N, skipped: N`.
  [GeneratedRegex(
    @"(?:^\s*total:\s*(\d+)\s*$|Test summary:\s*total:\s*(\d+))",
    RegexOptions.Multiline | RegexOptions.IgnoreCase)]
  private static partial Regex MtpTotalLine();

  [GeneratedRegex(
    @"(?:^\s*succeeded:\s*(\d+)\s*$|Test summary:.*?succeeded:\s*(\d+))",
    RegexOptions.Multiline | RegexOptions.IgnoreCase)]
  private static partial Regex MtpSucceededLine();

  [GeneratedRegex(
    @"(?:^\s*failed:\s*(\d+)\s*$|Test summary:.*?failed:\s*(\d+))",
    RegexOptions.Multiline | RegexOptions.IgnoreCase)]
  private static partial Regex MtpFailedLine();

  [GeneratedRegex(
    @"(?:^\s*skipped:\s*(\d+)\s*$|Test summary:.*?skipped:\s*(\d+))",
    RegexOptions.Multiline | RegexOptions.IgnoreCase)]
  private static partial Regex MtpSkippedLine();

  /// <summary>
  /// Parses Microsoft.Testing.Platform <c>dotnet test</c> summary (multi-line host lines or
  /// compact <c>Test summary: total: N, failed: N, succeeded: N, skipped: N</c>). Uses the last
  /// match of each. <c>failed:</c> is required; missing <c>skipped:</c> is 0.
  /// </summary>
  internal static bool TryParseMtpSummary(string plainOutput, out MtpSummary mtpSummary)
  {
    mtpSummary = default;

    MatchCollection totalMatches = MtpTotalLine().Matches(plainOutput);
    MatchCollection succeededMatches = MtpSucceededLine().Matches(plainOutput);
    MatchCollection failedMatches = MtpFailedLine().Matches(plainOutput);
    if (totalMatches.Count == 0 || succeededMatches.Count == 0 || failedMatches.Count == 0)
      return false;

    int total = ParseFirstCapturingGroup(totalMatches[^1]);
    int succeeded = ParseFirstCapturingGroup(succeededMatches[^1]);
    int failed = ParseFirstCapturingGroup(failedMatches[^1]);
    int skipped = 0;
    MatchCollection skippedMatches = MtpSkippedLine().Matches(plainOutput);
    if (skippedMatches.Count > 0)
      skipped = ParseFirstCapturingGroup(skippedMatches[^1]);

    mtpSummary = new MtpSummary(total, succeeded, failed, skipped);
    return true;
  }

  /// <summary>
  /// Decides whether a parsed MTP aggregator summary (or a failed parse) satisfies the
  /// zero-failure floor gate. Does not consider process exit code — that is a harness
  /// precondition.
  /// </summary>
  internal static AggregatorSummaryDecision Decide(MtpSummary? mtpSummary, int minimumSucceeded)
  {
    if (mtpSummary is not { } summary)
    {
      return new AggregatorSummaryDecision(
        AggregatorSummaryVerdict.FailUnparsable,
        "MTP summary could not be parsed (aggregator may have discovered zero tests)");
    }

    if (summary.Failed != 0)
    {
      return new AggregatorSummaryDecision(
        AggregatorSummaryVerdict.FailHasFailures,
        $"aggregator reported failed={summary.Failed}");
    }

    if (summary.Succeeded < minimumSucceeded)
    {
      return new AggregatorSummaryDecision(
        AggregatorSummaryVerdict.FailBelowFloor,
        $"succeeded {summary.Succeeded} is below floor {minimumSucceeded}");
    }

    if (summary.Total != summary.Succeeded + summary.Skipped)
    {
      return new AggregatorSummaryDecision(
        AggregatorSummaryVerdict.FailTotalMismatch,
        $"total {summary.Total} != succeeded {summary.Succeeded} + skipped {summary.Skipped}");
    }

    if (summary.Succeeded > 2 * minimumSucceeded)
    {
      return new AggregatorSummaryDecision(
        AggregatorSummaryVerdict.WarnStaleFloor,
        $"succeeded {summary.Succeeded} is more than 2× floor {minimumSucceeded}; raise MinimumSucceeded so the floor stays meaningful");
    }

    return new AggregatorSummaryDecision(AggregatorSummaryVerdict.Pass, string.Empty);
  }

  private static int ParseFirstCapturingGroup(Match match)
  {
    for (int i = 1; i < match.Groups.Count; i++)
    {
      if (match.Groups[i].Success)
        return int.Parse(match.Groups[i].Value, CultureInfo.InvariantCulture);
    }

    throw new InvalidOperationException("MTP summary regex matched without a capturing group.");
  }
}
