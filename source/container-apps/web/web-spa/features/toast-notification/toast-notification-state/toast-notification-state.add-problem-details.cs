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

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly INotificationService ToastService;
      public Handler
    (
      IStore store,
        INotificationService toastService
      ) : base(store)
    {
        ToastService = toastService;
      }

      public override async Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        if (action.SharedProblemDetails.Status == Constants.OperationCancelled) return;
        string message = action.SharedProblemDetails.Detail ?? "An error occurred";
        await ToastService.ShowToastAsync(options =>
        {
          options.Intent = ToastIntent.Error;
          options.Title = message;
        });
      }
    }
  }
}
