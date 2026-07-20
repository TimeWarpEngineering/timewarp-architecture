#region Purpose
// Marker interface tagging aggregate roots — the consistency boundary the domain invariants
// guard validates before every persisted change, and that TWA0011 requires to declare a nested
// Invariants validator.
#endregion

namespace TimeWarp.Foundation.Entities;

#pragma warning disable CA1040
public interface IAggregateRoot;
#pragma warning restore CA1040
