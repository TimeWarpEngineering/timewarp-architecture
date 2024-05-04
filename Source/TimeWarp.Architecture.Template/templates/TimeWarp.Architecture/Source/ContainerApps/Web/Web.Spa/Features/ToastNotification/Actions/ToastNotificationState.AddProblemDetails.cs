namespace TimeWarp.Architecture.Features.ToastNotifications;

internal partial class ToastNotificationState
{
  public static class AddProblemDetails
  {
    public sealed class Action : IBaseAction
    {
      public SharedProblemDetails SharedProblemDetails { get; }
      public Action(SharedProblemDetails sharedProblemDetails)
      {
        SharedProblemDetails = sharedProblemDetails;
      }
    }

    [UsedImplicitly]
    public class Handler
    (
      IStore store,
      IToastService ToastService
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
        ToastService.ShowError(action.SharedProblemDetails.Detail ?? "An error occurred");
        return Task.CompletedTask;
      }
    }
  }
}
