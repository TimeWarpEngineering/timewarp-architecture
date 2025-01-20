namespace TimeWarp.Architecture.Features.ToastNotifications;

partial class ToastNotificationState
{
  [UsedImplicitly]
  public static class AddNotification
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

    [UsedImplicitly]
    internal class Handler
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
        ToastService.ShowToast(action.Intent, action.Title);

        return Task.CompletedTask;
      }
    }
  }
}

