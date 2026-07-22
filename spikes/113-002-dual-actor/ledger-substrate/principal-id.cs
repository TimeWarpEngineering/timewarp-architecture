#region Purpose
// Stable per-principal identity for a credit ledger: a non-empty Guid that keys one single-writer
// balance.
#endregion

#region Design
// Hand-written readonly record struct rather than the repo's [TypedId] generator: the substrate
// deliberately does NOT reference the analyzer/generator package, so the spike's golden-pattern
// fidelity claim rests only on foundation-domain/-application source. A record struct already gives
// value equality + IEquatable<PrincipalId>, satisfying Entity<TId>'s `struct, IEquatable<TId>`
// constraint. In the real template this would be `[TypedId] readonly partial record struct`.
#endregion

namespace TimeWarp.Spike.DualActor;

public readonly record struct PrincipalId(Guid Value)
{
  public static PrincipalId New() => new(Guid.NewGuid());

  public bool IsEmpty => Value == Guid.Empty;

  public override string ToString() => Value.ToString();
}
