#region Purpose
// One referenceable resource: its type prefix, id, label, and the @ token Ask inserts.
#endregion

#region Design
// Task 345. Strings only, so store state clones it as a plain record. Id is the canonical text
// of the typed id (a Guid "D" string), which the [TypedId] parses and JSON reads, so the agent
// can pass it straight into an action parameter of IdTypeName. The label is display text and
// never part of the token.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A resource found by a referenceable list query.</summary>
public sealed record ResourceReferenceItem(string Prefix, string Id, string Label, string IdTypeName)
{
  /// <summary>The text inserted into the Ask draft: <c>@prefix:id</c>.</summary>
  public string Token => "@" + Prefix + ":" + Id;
}
