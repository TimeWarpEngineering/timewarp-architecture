#region Purpose
// A resource type as the @ picker lists it: prefix, display name, and id type, strings only.
#endregion

#region Design
// AskReferenceState clones on dispatch, so it holds this record and never ResourceReferenceType
// (which carries the search delegate). Handlers look the type up again by Prefix.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A permitted resource type row in the @ picker.</summary>
public sealed record ResourceReferenceTypeRow(string Prefix, string DisplayName, string IdTypeName);
