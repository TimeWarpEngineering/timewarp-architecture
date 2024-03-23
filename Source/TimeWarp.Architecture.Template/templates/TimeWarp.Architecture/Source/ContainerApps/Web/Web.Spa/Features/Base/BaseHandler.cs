namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract partial class BaseHandler<TAction>
(
  IStore store
) : ActionHandler<TAction>(store)
  where TAction : IAction;
