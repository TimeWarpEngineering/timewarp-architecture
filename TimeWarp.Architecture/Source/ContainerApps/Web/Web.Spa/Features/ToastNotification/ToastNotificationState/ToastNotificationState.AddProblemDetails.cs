namespace TimeWarp.Architecture.Features.ToastNotifications;

partial class ToastNotificationState
{
  internal static class AddProblemDetailsActionSet
  {
    internal sealed class Action : IBaseAction
    {
      public Action(SharedProblemDetails sharedProblemDetails)
      {
        SharedProblemDetails = sharedProblemDetails;
      }
      public SharedProblemDetails SharedProblemDetails { get; }
    }

    [UsedImplicitly]
    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IToastService ToastService;
      public Handler
    (
      IStore store,
        IToastService toastService
      ) : base(store)
    {
        ToastService = toastService;
      }

      public override Task Handle
      (
        Action action,
        CancellationToken aCancellationToken
      )
      {
        if (action.SharedProblemDetails.Status == Constants.OperationCancelled) return Task.CompletedTask;
        string message = action.SharedProblemDetails.Detail ?? "An error occurred";
        ToastService.ShowError(message, timeout:0);
        return Task.CompletedTask;
      }
    }
  }
}
