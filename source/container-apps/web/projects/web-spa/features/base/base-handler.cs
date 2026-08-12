#region Purpose
// Common root for all action handlers: shared typed state accessors over the store.
#endregion

#region Design
// Declared partial so feature code can add typed state accessor properties in sibling files
// instead of every handler calling Store.GetState<T>() inline.
// Sits at the root of the handler hierarchy (ApiHandler, AuthenticatedHandler derive from it) so
// cross-cutting handler conveniences have a single home.
#endregion

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
  protected BaseHandler(IStore store) : base(store) {}
  protected RouteState RouteState => Store.GetState<RouteState>();
}
