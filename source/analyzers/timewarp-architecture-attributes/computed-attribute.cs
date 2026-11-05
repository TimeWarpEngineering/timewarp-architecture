#region Purpose
// Marks a query response, or one of its members, as computed by the handler rather than projected
// from the aggregate root (TWA0041 skips it).
#endregion

#region Design
// TWA0041 requires each query-response member in a rooted operation group to map to a root member
// by exact name and compatible type. A value the handler derives (an avatar rendered from the user
// id, a count, a label) has no root member and must say so instead of inventing one on the root.
// On a property: that member is skipped. On the response class (or a list response's item class):
// the whole response is skipped. No reason argument: the member's name and XML doc carry what it is.
// Lives in TimeWarp.Architecture.Attributes, matched BY SIMPLE NAME by the convention analyzers.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that a query response member (or a whole response) is computed by the handler, not
/// projected from the operation group's aggregate root. TWA0041 does not check it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ComputedAttribute : Attribute;
