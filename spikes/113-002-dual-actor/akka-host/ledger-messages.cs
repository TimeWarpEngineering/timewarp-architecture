#region Purpose
// Akka message contract for the ledger actors: the Debit command routed by the coordinator to the
// per-principal child. Reply is the substrate's PostResult.
#endregion

namespace TimeWarp.Spike.DualActor.Akka;

public static class LedgerMessages
{
  public sealed record Debit(PrincipalId Id, long Amount);
}
