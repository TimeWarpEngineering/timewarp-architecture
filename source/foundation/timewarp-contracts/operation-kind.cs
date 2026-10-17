#region Purpose
// Transport-neutral operation kind, taken from the request's mediator interface.
#endregion

#region Design
// Three values match TimeWarp.Mediator.IQuery`1, IIdempotentCommand / IIdempotentCommand`1,
// and ICommand / ICommand`1. Generators test IIdempotentCommand before ICommand because the
// idempotent interfaces extend the command interfaces. A request with none of them is left
// out of the catalog. This enum does not choose an HTTP verb.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// Transport-neutral operation kind. Generators set it from the request's mediator interface.
/// </summary>
public enum OperationKind
{
  /// <summary>The request implements <c>TimeWarp.Mediator.IQuery&lt;TResponse&gt;</c>.</summary>
  Query,

  /// <summary>The request implements <c>TimeWarp.Mediator.ICommand</c> or <c>ICommand&lt;TResponse&gt;</c>.</summary>
  Command,

  /// <summary>
  /// The request implements <c>TimeWarp.Mediator.IIdempotentCommand</c> or
  /// <c>IIdempotentCommand&lt;TResponse&gt;</c>.
  /// </summary>
  IdempotentCommand
}
