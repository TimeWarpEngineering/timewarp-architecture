namespace TimeWarp.Architecture.Features;

internal abstract partial class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  protected SuperheroState SuperheroState => Store.GetState<SuperheroState>();
}
