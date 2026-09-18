#region Purpose
// ApplicationState action that makes the modal with the given id the active modal.
#endregion

#region Design
// A single app-wide ActiveModalId (rather than per-modal open flags) enforces that at most
// one modal shows at a time.
// The string id is the contract with ModalController/ModalContainer, which match against it
// to render and fire OnActivate; CloseModalActionSet nulls it to dismiss.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class ApplicationState
{
  public static class SetActiveModalActionSet
  {
    public class Action : IBaseAction
    {
      public string ModalId { get; }
      public Action(string modalId)
      {
        ModalId = modalId;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        ApplicationState.ActiveModalId = action.ModalId;
        return default;
      }
    }
  }
}
