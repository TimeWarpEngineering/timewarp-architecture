namespace TimeWarp.Architecture.Features;

using TimeWarp.Architecture.Features.EventStreams;

internal abstract partial class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  protected EventStreamState EventStreamState => Store.GetState<EventStreamState>();
}
