namespace TimeWarp.Architecture.Features.ToastNotifications;

partial class ToastNotificationState
{

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


    internal class Handler
    (
      IStore store,
      IToastService ToastService
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

