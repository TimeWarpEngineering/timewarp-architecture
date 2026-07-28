#region Purpose
// AddProblemDetails action: surfaces API failures (SharedProblemDetails) as error toasts.
#endregion

#region Design
// This is the single failure UX for all API calls: DefaultApiHandler.HandleError dispatches it,
// so individual product-slice handlers need no error-display code.
// OperationCancelled status is swallowed — user-initiated cancellation is not an error and
// would otherwise produce noise toasts.
// Internal (unlike AddNotificationActionSet) because only pipeline/base-handler code should
// raise it; components report errors by returning problem details, not by dispatching this.
#endregion

namespace TimeWarp.Architecture.Features;

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
