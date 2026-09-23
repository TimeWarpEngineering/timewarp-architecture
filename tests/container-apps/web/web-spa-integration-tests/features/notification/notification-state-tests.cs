#region Purpose
// NotificationState: one shape (Title/Body), dedupe, visible cap, navigation clear, and Success auto-dismiss.
#endregion

#region Design
// Task 247 rules 2–4 through the real SPA pipeline (session-shared Aspire host, per-test
// SpaTestScope). Dedupe is exercised both ways a failure reaches the region: the page path
// (ReportProblem action) and the handler path (ProblemDetailsNotification published on
// IPublisher<ClientPipeline>) — the same problem must yield one bar. Navigation drives the
// existing RouteState.ChangeRoute action into TestNavigationManager so the
// NavigationListener's LocationChanged hook is the thing under test, not a direct Clear.
// Auto-dismiss is deterministic: ExpireMessages takes the clock value, so the test passes a
// future instant instead of sleeping.
#endregion

namespace NotificationState_;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Features.Routing;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class Should
{
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    // Session-owned: the Jaribu session hook disposes SpaSessionFixture; do not dispose here.
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  private static SharedProblemDetails AlreadyOnThisAccount() =>
    new()
    {
      Status = 409,
      Title = "Already on this account",
      Detail = "This passkey is already on this account."
    };

  public static async Task Put_Problem_Title_And_Detail_In_Title_And_Body()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    await scope.Send(new ReportProblemActionSet.Action(AlreadyOnThisAccount()));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.Messages.Count.ShouldBe(1);
    state.Messages[0].Intent.ShouldBe(MessageBarIntent.Error);
    state.Messages[0].Title.ShouldBe("Already on this account");
    state.Messages[0].Body.ShouldBe("This passkey is already on this account.");
    state.Messages[0].AutoDismissAt.ShouldBeNull();
  }

  public static async Task Drop_Body_When_Detail_Repeats_Title()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    await scope.Send
    (
      new ReportProblemActionSet.Action
      (
        new SharedProblemDetails { Status = 400, Title = "Passkey rejected.", Detail = "passkey rejected." }
      )
    );

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.Messages.Count.ShouldBe(1);
    state.Messages[0].Title.ShouldBe("Passkey rejected.");
    state.Messages[0].Body.ShouldBeNull();
  }

  public static async Task Render_The_Same_Problem_Once_When_Reported_By_Page_And_Handler()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    // Page path: component dispatches ReportProblem.
    await scope.Send(new ReportProblemActionSet.Action(AlreadyOnThisAccount()));
    // Handler path: DefaultApiHandler.HandleError publishes ProblemDetailsNotification.
    IPublisher<ClientPipeline> publisher =
      scope.ServiceProvider.GetRequiredService<IPublisher<ClientPipeline>>();
    await publisher.Publish(new ProblemDetailsNotification(AlreadyOnThisAccount()));
    // And the page again.
    await scope.Send(new ReportProblemActionSet.Action(AlreadyOnThisAccount()));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.Messages.Count.ShouldBe(1);
    state.Messages[0].Title.ShouldBe("Already on this account");
  }

  public static async Task Keep_Distinct_Messages_And_Cap_The_Visible_Stack()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    for (int i = 1; i <= MaxVisible + 2; i++)
    {
      await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, $"Failure {i}"));
    }

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.Messages.Count.ShouldBe(MaxVisible + 2);
    state.VisibleMessages.Count.ShouldBe(MaxVisible);
    state.HiddenCount.ShouldBe(2);
    // Newest stay visible; the oldest are the ones collapsed behind "+N more".
    state.VisibleMessages[^1].Title.ShouldBe($"Failure {MaxVisible + 2}");
    state.VisibleMessages[0].Title.ShouldBe("Failure 3");
  }

  public static async Task Clear_Errors_When_The_Route_Changes()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    // Resolving the listener is what Routes.razor's @inject does in the app.
    NavigationListener listener = scope.ServiceProvider.GetRequiredService<NavigationListener>();

    await scope.Send(new ReportProblemActionSet.Action(AlreadyOnThisAccount()));
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(1);

    await scope.Send(new RouteState.ChangeRouteActionSet.Action("/Counter"));
    await listener.LastDispatch;

    scope.Store.GetState<NotificationState>().Messages.ShouldBeEmpty();
    scope.ServiceProvider.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/Counter");
  }

  public static async Task Auto_Dismiss_Success_But_Keep_Errors()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    DateTimeOffset before = DateTimeOffset.UtcNow;

    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Success, "Passkey added."));
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Passkey rejected."));

    NotificationState state = scope.Store.GetState<NotificationState>();
    NotificationMessage success = state.Messages.Single(message => message.Intent == MessageBarIntent.Success);
    success.Title.ShouldBe("Passkey added.");
    success.AutoDismissAt.ShouldNotBeNull();
    success.AutoDismissAt.Value.ShouldBeGreaterThanOrEqualTo(before + SuccessAutoDismissInterval);
    state.NextExpiry.ShouldBe(success.AutoDismissAt);

    // Not yet due: nothing expires.
    await scope.Send(new ExpireMessagesActionSet.Action(DateTimeOffset.UtcNow));
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(2);

    // Past the interval: the Success bar goes, the Error bar stays until dismissed.
    await scope.Send
    (
      new ExpireMessagesActionSet.Action(success.AutoDismissAt.Value + TimeSpan.FromSeconds(1))
    );
    state = scope.Store.GetState<NotificationState>();
    state.Messages.Count.ShouldBe(1);
    state.Messages[0].Intent.ShouldBe(MessageBarIntent.Error);
    state.NextExpiry.ShouldBeNull();

    await scope.Send(new DismissMessageActionSet.Action(state.Messages[0].Id));
    scope.Store.GetState<NotificationState>().Messages.ShouldBeEmpty();
  }
}
