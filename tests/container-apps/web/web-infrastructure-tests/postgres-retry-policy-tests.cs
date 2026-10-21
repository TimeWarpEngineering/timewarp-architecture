#region Purpose
// PostgresRetryPolicy.Resolve parses Postgres:RetryCount and Postgres:RetryDelaySeconds.
#endregion

#region Design
// RetryDelaySeconds is EF's maximum back-off, stored as a TimeSpan of whole seconds. A blank or
// unparsable value keeps MaxRetryCount and MaxRetryDelay. The module calls Configure with the
// result, so this parsing is the production path.
#endregion

namespace PostgresRetryPolicy_;

public class Resolve_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Resolve_Given_>();

  public static Task Defaults_Should_MatchThePolicyConstants()
  {
    (int count, TimeSpan maximumDelay) = PostgresRetryPolicy.Resolve(null, null);
    count.ShouldBe(PostgresRetryPolicy.MaxRetryCount);
    maximumDelay.ShouldBe(PostgresRetryPolicy.MaxRetryDelay);
    return Task.CompletedTask;
  }

  public static Task SetValues_Should_OverrideBothBounds()
  {
    (int count, TimeSpan maximumDelay) = PostgresRetryPolicy.Resolve("6", "8");
    count.ShouldBe(6);
    maximumDelay.ShouldBe(TimeSpan.FromSeconds(8));
    PostgresRetryPolicy.Resolve("4", "0").MaximumDelay.ShouldBe(TimeSpan.Zero);
    return Task.CompletedTask;
  }

  public static Task OneKnob_Should_LeaveTheOtherAtTheDefault()
  {
    (int count, TimeSpan maximumDelay) = PostgresRetryPolicy.Resolve("6", null);
    count.ShouldBe(6);
    maximumDelay.ShouldBe(PostgresRetryPolicy.MaxRetryDelay);

    (int defaultCount, TimeSpan overriddenDelay) = PostgresRetryPolicy.Resolve(null, "8");
    defaultCount.ShouldBe(PostgresRetryPolicy.MaxRetryCount);
    overriddenDelay.ShouldBe(TimeSpan.FromSeconds(8));
    return Task.CompletedTask;
  }

  public static Task Unparsable_Should_KeepTheDefault()
  {
    (int count, TimeSpan maximumDelay) = PostgresRetryPolicy.Resolve("nope", "8.5");
    count.ShouldBe(PostgresRetryPolicy.MaxRetryCount);
    maximumDelay.ShouldBe(PostgresRetryPolicy.MaxRetryDelay);

    (int negativeCount, TimeSpan blankDelay) = PostgresRetryPolicy.Resolve("-1", " 8 ");
    negativeCount.ShouldBe(PostgresRetryPolicy.MaxRetryCount);
    blankDelay.ShouldBe(PostgresRetryPolicy.MaxRetryDelay);

    PostgresRetryPolicy.Resolve("", null).Count.ShouldBe(PostgresRetryPolicy.MaxRetryCount);
    return Task.CompletedTask;
  }
}
