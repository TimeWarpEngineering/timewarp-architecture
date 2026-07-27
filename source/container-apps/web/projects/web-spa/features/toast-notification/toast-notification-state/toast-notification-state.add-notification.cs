#region Purpose
// AddNotification action: raises an arbitrary toast with caller-chosen intent and title.
#endregion

#region Design
// Exists so components raise toasts by dispatching an action instead of injecting FluentUI's
// INotificationService directly — display stays behind the mediator pipeline and swappable.
// The handler delegates entirely to the toast service and writes nothing to state; FluentUI
// owns toast lifetime and rendering (see the root partial's rationale).
#endregion

namespace TimeWarp.Architecture.Features;

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

