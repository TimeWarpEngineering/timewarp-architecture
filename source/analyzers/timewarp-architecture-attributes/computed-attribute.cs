#region Purpose
// Marks a query response, or one of its members, as computed by the handler rather than projected
// from the aggregate root (TWA0041 skips it).
#endregion

#region Design
// TWA0041 requires each query-response member in a rooted operation group to map to a root member
// by exact name and compatible type. A value the handler derives (an avatar rendered from the user
// id, a count, a label) has no root member and must say so instead of inventing one on the root.
// The attribute is for values with no root member. A member that projects from the root but that
// the handler may null for one audience (redaction) is not computed: the response declares it T?
// and TWA0041 accepts T? against the root's T (read widening, ContractAgreementTypes Design).
// On a property: that member is skipped. On a class the walk visits, the whole class is skipped:
// the response class, a list response's item class, a child class walked against a root child
// (an activity entry), or a wrapped projection class (an admin view). No reason argument: the
// member's name and XML doc carry what it is.
// Lives in TimeWarp.Architecture.Attributes, matched BY SIMPLE NAME by the convention analyzers.
#endregion

namespace TimeWarp.Architecture.Attributes;

/// <summary>
/// Declares that a query response member, or a whole class TWA0041 walks (the response, a list
/// item, a child class, or a wrapped projection class), is computed by the handler, not projected
/// from the operation group's aggregate root. TWA0041 does not check it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ComputedAttribute : Attribute;
