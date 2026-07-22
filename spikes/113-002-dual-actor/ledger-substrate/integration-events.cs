#region Purpose
// Substrate-agnostic integration-event seam: the abstract publish port both hosts emit through, a
// recording in-memory implementation for the spike, and the LedgerEntryPosted event itself.
#endregion

#region Design
// This is the axis-3 "publish seam" the spike must exercise to prove the LOSING candidate leaves no
// residue: the Akka actor and the Orleans grain each depend only on IIntegrationEventPublisher, so
// swapping the host swaps zero event-publishing code. The recording implementation lets the
// concurrency tests assert "50 recorded events" without a real broker. In the template this port
// would be backed by the real substrate (outbox / bus); here it is a thread-safe list.
#endregion

namespace TimeWarp.Spike.DualActor;

using System.Collections.Concurrent;

public interface IIntegrationEventPublisher
{
  Task PublishAsync(object integrationEvent, CancellationToken cancellationToken = default);
}

public sealed record LedgerEntryPosted(PrincipalId PrincipalId, long Amount, long NewBalance, long Version);

public sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
  private readonly ConcurrentQueue<object> published = new();

  public IReadOnlyCollection<object> Published => published;

  public int CountOf<TEvent>() => published.OfType<TEvent>().Count();

  public Task PublishAsync(object integrationEvent, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(integrationEvent);
    published.Enqueue(integrationEvent);
    return Task.CompletedTask;
  }
}
