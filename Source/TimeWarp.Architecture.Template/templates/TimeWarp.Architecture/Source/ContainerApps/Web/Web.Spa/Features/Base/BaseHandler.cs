namespace TimeWarp.Architecture.Features;

using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Counters;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  protected ApplicationState ApplicationState => Store.GetState<ApplicationState>();

  protected CounterState CounterState => Store.GetState<CounterState>();

  public BaseHandler(IStore aStore) : base(aStore) { }
}
