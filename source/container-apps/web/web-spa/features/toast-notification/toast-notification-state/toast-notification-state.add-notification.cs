namespace TimeWarp.Architecture.Features.ToastNotifications;

partial class ToastNotificationState
{

  // Named ...ActionSet so the TimeWarp.State ActionSetMethodSourceGenerator emits a strongly-typed
  // dispatcher: `ToastNotificationState.AddNotification(intent, title)` — matching AddProblemDetailsActionSet.
  public static class AddNotificationActionSet
  {
    public sealed class Action : IBaseAction
    {
      public ToastIntent Intent { get; }
      public string Title { get;  }

      public Action
      (
        ToastIntent intent,
        string title
      )
      {
        Intent = intent;
        Title = title;
      }
    }

    internal class Handler
    (
      IStore store,
      INotificationService ToastService
    ) : BaseHandler<Action>(store)
    {

      public override async Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        await ToastService.ShowToastAsync(options =>
        {
          options.Intent = action.Intent;
          options.Title = action.Title;
        });
      }
    }
  }
}

