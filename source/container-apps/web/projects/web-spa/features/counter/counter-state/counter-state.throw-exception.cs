#region Purpose
// CounterState action whose handler always throws, to exercise exception handling.
#endregion

#region Design
// Exists so demos and tests can observe how unhandled handler exceptions propagate
// through the TimeWarp.State pipeline and surface in the UI; never dispatch it from
// production flows.
// Named ...ActionSet with an explicit Action constructor so the TimeWarp.State
// ActionSetMethodSourceGenerator emits `CounterState.ThrowException(message)` — the generator
// reads ConstructorDeclarationSyntax only, so a primary constructor yields no usable dispatcher.
// Callers must use that generated method: TWA0022 bans direct mediator Send in SPA client code.
#endregion

namespace TimeWarp.Architecture.Features.Counters;

partial class CounterState
{
  public static class ThrowExceptionActionSet
  {
    public class Action : IBaseAction
    {
      public string Message { get; }

      public Action(string message)
      {
        Message = message;
      }
    }

    internal class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {

      public override Task Handle
      (
        Action action,
        CancellationToken cancellationToken
      ) =>
        // Intentionally throw so we can test exception handling.
        throw new Exception(action.Message);
    }
  }
}
