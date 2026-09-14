#region Purpose
// AnalyticsState.Clone identity and value semantics under an in-proc SPA test host.
#endregion

#region Design
// C-create AnalyticsSpaTestApplication: clone does not need a live BFF. CorrelationId is
// state data and must round-trip; Guid is clone identity and must differ.
#endregion

namespace AnalyticsState_;

using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;

[TestTag("Integration")]
public class Clone_Should
{
  private static AnalyticsSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Clone_Should>();

  public static Task SetupOnce()
  {
    Spa = new AnalyticsSpaTestApplication();
    return Task.CompletedTask;
  }

  public static Task CleanUpOnce()
  {
    Spa?.Dispose();
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Clone()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    AnalyticsState analyticsState = scope.Store.GetState<AnalyticsState>();
    Guid correlationId = Guid.NewGuid();
    analyticsState.Initialize(correlationId);

    AnalyticsState clone = analyticsState.Clone();

    analyticsState.ShouldNotBeSameAs(clone);
    analyticsState.CorrelationId.ShouldBe(clone.CorrelationId);
    analyticsState.Guid.ShouldNotBe(clone.Guid);
    return Task.CompletedTask;
  }
}
