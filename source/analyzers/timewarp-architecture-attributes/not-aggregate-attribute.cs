#region Purpose
// Reasoned opt-out from the contract-agreement checks (TWA0039–TWA0042) for one operation in a
// group that an aggregate root satisfies.
#endregion

#region Design
// ContractAgreementAnalyzer links an aggregate root to an [Operation] group by slice namespace and
// then requires every operation in the group to be accounted for: a command implements the details
// interface or is a Set-list command, a query's response projects from the root. An operation
// that sits in the group for host grouping but does not read or write the root (the user's xAI key
// under "profile"), or that carries no root data (a delete by route id), says so here. The reason
// is required: whitespace does not opt out, so the skip is a stated decision.
// The attribute goes on the request type (the nested Command or Query that carries [Operation]).
// It removes that operation from every agreement check; it does not unlink the group.
// Lives in TimeWarp.Architecture.Attributes, matched BY SIMPLE NAME by the convention analyzers,
// which must not ProjectReference this assembly.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that this operation is not checked against its group's aggregate root.
/// Suppresses TWA0039–TWA0042 for the operation only when the reason is non-empty and non-whitespace.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NotAggregateAttribute : Attribute
{
  /// <summary>Why this operation does not agree with the group's aggregate root. Whitespace does not opt out.</summary>
  public string Reason { get; }

  /// <summary>Opts this operation out of the contract-agreement checks when <paramref name="reason"/> is non-empty.</summary>
  public NotAggregateAttribute(string reason)
  {
    Reason = reason;
  }
}
