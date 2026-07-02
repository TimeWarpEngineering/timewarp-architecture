#region Purpose
// Marker interface tagging domain invariant rule sets (the nested AbstractValidator classes on entities) so they are identifiable as invariants rather than request validators.
#endregion

namespace TimeWarp.Architecture.Abstractions;

/// <summary>
/// A marker interface for all invariant rules.
/// </summary>
#pragma warning disable CA1040
public interface IInvariants { }
#pragma warning restore CA1040
