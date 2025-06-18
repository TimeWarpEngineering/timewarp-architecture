namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract partial class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  /// <summary>
  /// Base Handler that makes it easy to access state
  /// </summary>
  /// <typeparam name="TAction"></typeparam>
  protected BaseHandler(IStore store) : base(store) {}
  protected RouteState RouteState => Store.GetState<RouteState>();
}
