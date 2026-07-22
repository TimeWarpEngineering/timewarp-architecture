#region Purpose
// Supervisor that owns one LedgerActor child per principal and routes each Debit to the right child
// (creating it on first use). This is the "which actor handles this id" layer Akka makes explicit —
// there is no virtual-actor auto-activation, so the routing/lifetime is hand-written here.
#endregion

#region Design
// Child name = principal id string; Context.Child(name) is the lookup, Context.ActorOf the create.
// Forward (not Tell) preserves the original Sender so the caller's Ask receives the child's reply.
// Default supervision (restart-on-exception) is fine for the spike; no custom SupervisorStrategy.
// This explicit coordinator is one of the measured ceremony differences vs Orleans, where the grain
// runtime resolves an id to a grain with no equivalent hand-written router.
#endregion

namespace TimeWarp.Spike.DualActor.Akka;

using global::Akka.Actor;
using Microsoft.EntityFrameworkCore;

public sealed class LedgerCoordinator : ReceiveActor
{
  private readonly IDbContextFactory<LedgerDbContext> contextFactory;
  private readonly IIntegrationEventPublisher publisher;

  public LedgerCoordinator(IDbContextFactory<LedgerDbContext> contextFactory, IIntegrationEventPublisher publisher)
  {
    this.contextFactory = contextFactory;
    this.publisher = publisher;

    Receive<LedgerMessages.Debit>(command => ChildFor(command.Id).Forward(command));
  }

  private IActorRef ChildFor(PrincipalId id)
  {
    string name = id.Value.ToString();
    IActorRef existing = Context.Child(name);
    if (!existing.Equals(ActorRefs.Nobody)) return existing;

    return Context.ActorOf(
      Props.Create(() => new LedgerActor(id, contextFactory, publisher)), name);
  }
}
