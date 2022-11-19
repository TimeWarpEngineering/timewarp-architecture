namespace TimeWarp.Architecture.Features;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract partial class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  protected ApplicationState ApplicationState => Store.GetState<ApplicationState>();

  protected CounterState CounterState => Store.GetState<CounterState>();

  protected EventStreamState EventStreamState => Store.GetState<EventStreamState>();
  
  public BaseHandler(IStore aStore) : base(aStore) { }
}
