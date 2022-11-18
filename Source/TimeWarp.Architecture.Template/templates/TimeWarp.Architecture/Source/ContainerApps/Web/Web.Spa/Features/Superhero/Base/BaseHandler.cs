namespace TimeWarp.Architecture.Features;

using TimeWarp.Architecture.Features.Superheros;

/// <summary>
/// Base Handler that makes it easy to access state
/// </summary>
/// <typeparam name="TAction"></typeparam>
internal abstract partial class BaseHandler<TAction> : ActionHandler<TAction>
  where TAction : IAction
{
  protected SuperheroState SuperheroState => Store.GetState<SuperheroState>();
}
