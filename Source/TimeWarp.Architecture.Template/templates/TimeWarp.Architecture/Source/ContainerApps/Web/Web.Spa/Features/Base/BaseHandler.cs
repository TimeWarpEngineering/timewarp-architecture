namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  public BaseHandler(IStore aStore) : base(aStore) { }
}
