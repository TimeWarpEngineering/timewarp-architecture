#region Purpose
// Equatable data the [ContractState] generator and the ActionRoster generator share: one state and its contracts.
#endregion

#region Design
// Plain strings, bools and EquatableArray, so the incremental pipeline caches across unrelated edits. Type names are
// fully qualified (global::) when they are written into source. Diagnostics carry their location as path and spans,
// not a Location, so the model stays a value.
#endregion

namespace TimeWarp.Architecture.Analyzers;

/// <summary>One state that carries [ContractState], with everything the emitter writes for it.</summary>
internal sealed record ContractStateModel
(
  string? Namespace,
  EquatableArray<string> TypeHeaders,
  string StateName,
  string StateType,
  string HintName,
  string ActionBaseType,
  string HandlerBaseType,
  string StoreType,
  string PublisherType,
  string ApiServiceType,
  string? ConfirmationServiceType,
  string? TrackActionType,
  EquatableArray<ContractActionModel> Actions,
  EquatableArray<ContractStateDiagnostic> Diagnostics
);

/// <summary>One contract of a [ContractState] state: the ActionSet, dispatch overload and hooks it gets.</summary>
/// <param name="Name">The contract's simple name, for example GetRoles. Names the ActionSet, the method and the hooks.</param>
/// <param name="RequestType">The contract's Query or Command, fully qualified.</param>
/// <param name="ResponseType">The contract's Response, fully qualified.</param>
/// <param name="IsQuery">True when the request is a Query: the success hook is the required On…Loaded.</param>
/// <param name="IsDraft">Listed in DraftRequests: the request build is the required Build…Request hook.</param>
/// <param name="IsDestructive">The request or contract carries [Destructive]: Confirm…Async is required.</param>
/// <param name="RequestIsOptional">A Query with a public parameterless constructor: the dispatch overload defaults it.</param>
/// <param name="CopiedMembers">Public settable request properties copied into the request that is sent.</param>
internal sealed record ContractActionModel
(
  string Name,
  string RequestType,
  string ResponseType,
  bool IsQuery,
  bool IsDraft,
  bool IsDestructive,
  bool RequestIsOptional,
  EquatableArray<string> CopiedMembers
)
{
  public string ActionSetName => Name + "ActionSet";
}

/// <summary>A TWE015 report with its location as plain values.</summary>
internal sealed record ContractStateDiagnostic(string Message, string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
  public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}
