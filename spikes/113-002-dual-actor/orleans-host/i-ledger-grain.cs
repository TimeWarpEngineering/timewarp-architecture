#region Purpose
// Orleans grain contract for the ledger: a Guid-keyed (= principal id) virtual actor exposing Debit.
// The reply is a grain-boundary DTO the Orleans serializer owns.
#endregion

#region Design
// SUBSTRATE-RESIDUE FINDING (Orleans side): the substrate's own PostResult cannot be returned across
// the grain boundary — Orleans 10 copies/serializes every grain call's arguments and return value
// (even in a single local silo), and that requires the type to carry [GenerateSerializer] + [Id]
// members, which the substrate deliberately does NOT (it must stay Orleans-agnostic to prove the
// losing candidate leaves no residue). So the boundary needs an Orleans-owned mirror type
// (LedgerPosted) declared HERE and mapped back to PostResult in the host. Akka needs no equivalent:
// its local Ask passes CLR references with no serialization contract. This grain-local DTO is the
// price Orleans charges for its location-transparency model — recorded for the write-up.
// IGrainWithGuidKey: the grain key is the principal's Guid, so the runtime resolves an id straight
// to a grain with no hand-written coordinator/router (contrast the Akka LedgerCoordinator).
#endregion

namespace TimeWarp.Spike.DualActor.Orleans;

using global::Orleans;

public interface ILedgerGrain : IGrainWithGuidKey
{
  Task<LedgerPosted> Debit(long amount);
}

[GenerateSerializer]
public sealed record LedgerPosted([property: Id(0)] long Balance, [property: Id(1)] long Version);
