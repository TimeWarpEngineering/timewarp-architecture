// ReSharper disable InconsistentNaming
namespace JaribuAggregatorSummaryGate_;

public class Decide_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Decide_Given_>();

  public static Task OneSucceeded_Should_Pass()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 1, Succeeded: 1, Failed: 0, Skipped: 0));

    aggregatorSummaryDecision.Passed.ShouldBeTrue();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.Pass);
    return Task.CompletedTask;
  }

  public static Task LargeTotals_Should_Pass()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 361, Succeeded: 361, Failed: 0, Skipped: 0));

    aggregatorSummaryDecision.Passed.ShouldBeTrue();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.Pass);
    return Task.CompletedTask;
  }

  public static Task AnyFailed_Should_Fail()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 180, Succeeded: 179, Failed: 1, Skipped: 0));

    aggregatorSummaryDecision.Passed.ShouldBeFalse();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.FailHasFailures);
    return Task.CompletedTask;
  }

  public static Task UnparsableSummary_Should_Fail()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      mtpSummary: null);

    aggregatorSummaryDecision.Passed.ShouldBeFalse();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.FailUnparsable);
    return Task.CompletedTask;
  }

  public static Task ZeroTotal_Should_Fail()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 0, Succeeded: 0, Failed: 0, Skipped: 0));

    aggregatorSummaryDecision.Passed.ShouldBeFalse();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.FailZeroTotal);
    return Task.CompletedTask;
  }

  public static Task TotalNotSucceededPlusSkipped_Should_Fail()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 181, Succeeded: 180, Failed: 0, Skipped: 0));

    aggregatorSummaryDecision.Passed.ShouldBeFalse();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.FailTotalMismatch);
    return Task.CompletedTask;
  }

  public static Task SkippedBalanced_Should_Pass()
  {
    AggregatorSummaryDecision aggregatorSummaryDecision = JaribuAggregatorSummaryGate.Decide(
      new MtpSummary(Total: 182, Succeeded: 180, Failed: 0, Skipped: 2));

    aggregatorSummaryDecision.Passed.ShouldBeTrue();
    aggregatorSummaryDecision.AggregatorSummaryVerdict.ShouldBe(AggregatorSummaryVerdict.Pass);
    return Task.CompletedTask;
  }
}

public class TryParseMtpSummary_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryParseMtpSummary_Given_>();

  public static Task MultilineHostLines_Should_ParseAllFour()
  {
    const string output =
      """
      total: 180
      failed: 0
      succeeded: 180
      skipped: 2
      """;

    bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(output, out MtpSummary mtpSummary);

    parsed.ShouldBeTrue();
    mtpSummary.ShouldBe(new MtpSummary(Total: 180, Succeeded: 180, Failed: 0, Skipped: 2));
    return Task.CompletedTask;
  }

  public static Task CompactTestSummary_Should_ParseAllFour()
  {
    const string output =
      "Test summary: total: 9, failed: 0, succeeded: 9, skipped: 1, duration: 1s 234ms";

    bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(output, out MtpSummary mtpSummary);

    parsed.ShouldBeTrue();
    mtpSummary.ShouldBe(new MtpSummary(Total: 9, Succeeded: 9, Failed: 0, Skipped: 1));
    return Task.CompletedTask;
  }

  public static Task MissingSkipped_Should_TreatAsZero()
  {
    const string output =
      """
      total: 3
      failed: 0
      succeeded: 3
      """;

    bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(output, out MtpSummary mtpSummary);

    parsed.ShouldBeTrue();
    mtpSummary.ShouldBe(new MtpSummary(Total: 3, Succeeded: 3, Failed: 0, Skipped: 0));
    return Task.CompletedTask;
  }

  public static Task MissingFailed_Should_BeUnparsable()
  {
    const string output =
      """
      total: 180
      succeeded: 180
      skipped: 0
      """;

    bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(output, out _);

    parsed.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task EmptyOutput_Should_BeUnparsable()
  {
    bool parsed = JaribuAggregatorSummaryGate.TryParseMtpSummary(string.Empty, out _);

    parsed.ShouldBeFalse();
    return Task.CompletedTask;
  }
}
