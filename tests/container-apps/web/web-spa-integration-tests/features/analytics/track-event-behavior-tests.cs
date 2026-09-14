#region Purpose
// TrackEventBehavior: tagged actions POST once, untagged actions do not, API failures do not
// fail the traced action.
#endregion

#region Design
// C-create AnalyticsSpaTestApplication (recording IWebServerApiService) rather than
// SpaSessionFixture: these facts assert the POST, which requires DI substitution.
// IncrementCounterActionSet.Action is the tagged demo; ToggleMenu.Action is an untagged
// IAction that still succeeds. Re-fetch CounterState after Send — clone-on-dispatch replaces
// the instance. EventName is the action FullName because nested Action types all share Name
// "Action".
#endregion

namespace TrackEventBehavior_;

using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Analytics;
using static TimeWarp.Architecture.Features.Applications.ApplicationState;
using static TimeWarp.Architecture.Features.Counters.CounterState;
using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

[TestTag("Integration")]
public class Handle_Should
{
  private static AnalyticsSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Handle_Should>();

  public static Task SetupOnce()
  {
    Spa = new AnalyticsSpaTestApplication();
    return Task.CompletedTask;
  }

  public static Task Setup()
  {
    Spa!.Recording.Reset();
    return Task.CompletedTask;
  }

  public static Task CleanUpOnce()
  {
    Spa?.Dispose();
    Spa = null;
    return Task.CompletedTask;
  }

  public static async Task Post_Once_Given_TaggedAction()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    Guid correlationId = Guid.NewGuid();
    scope.Store.GetState<AnalyticsState>().Initialize(correlationId);
    scope.Store.GetState<CounterState>().Initialize(count: 22);

    await scope.Send(new IncrementCounterActionSet.Action(amount: 5));

    scope.Store.GetState<CounterState>().Count.ShouldBe(27);
    Spa!.Recording.Requests.Count.ShouldBe(1);
    Command command = Spa.Recording.Requests[0].ShouldBeOfType<Command>();
    command.EventName.ShouldBe(typeof(IncrementCounterActionSet.Action).FullName);
    command.CorrelationId.ShouldBe(correlationId);
  }

  public static async Task NotPost_Given_UntaggedAction()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    await scope.Send(new ToggleMenu.Action());

    Spa!.Recording.Requests.ShouldBeEmpty();
  }

  public static async Task NotFailTracedAction_Given_ApiException()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.Store.GetState<CounterState>().Initialize(count: 22);
    Spa!.Recording.ExceptionToThrow = new HttpRequestException("analytics unavailable");

    await scope.Send(new IncrementCounterActionSet.Action(amount: 5));

    scope.Store.GetState<CounterState>().Count.ShouldBe(27);
  }
}
