#region Purpose
// Thread-safe in-memory IAgentHumanLinkStore for zero-infra / skip-mode.
#endregion

#region Design
// ConcurrentDictionary keyed by AgentHumanLinkId; process-lifetime singleton matches
// InMemoryProfileStore. PostgresDbModule swaps to scoped EfAgentHumanLinkStore when connected.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using System.Collections.Concurrent;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;

/// <summary>In-memory AgentHumanLink store (zero-infra default).</summary>
public sealed class InMemoryAgentHumanLinkStore : IAgentHumanLinkStore
{
  private readonly ConcurrentDictionary<AgentHumanLinkId, AgentHumanLink> Links = new();

  public Task<AgentHumanLink?> FindAsync(AgentHumanLinkId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    Links.TryGetValue(id, out AgentHumanLink? link);
    return Task.FromResult(link);
  }

  public Task<AgentHumanLink?> FindOpenAsync(
    PrincipalId agentPrincipalId,
    PrincipalId humanPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    AgentHumanLink? open = Links.Values.FirstOrDefault(link =>
      link.AgentPrincipalId == agentPrincipalId.Value
      && link.HumanPrincipalId == humanPrincipalId.Value
      && link.Status is AgentHumanLinkStatus.Pending or AgentHumanLinkStatus.Approved);
    return Task.FromResult(open);
  }

  public Task AddAsync(AgentHumanLink link, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);
    cancellationToken.ThrowIfCancellationRequested();
    if (!Links.TryAdd(link.Id, link))
    {
      throw new InvalidOperationException($"AgentHumanLink '{link.Id}' already exists.");
    }

    return Task.CompletedTask;
  }

  public Task UpdateAsync(AgentHumanLink link, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);
    cancellationToken.ThrowIfCancellationRequested();
    if (!Links.ContainsKey(link.Id))
    {
      throw new InvalidOperationException($"AgentHumanLink '{link.Id}' does not exist.");
    }

    Links[link.Id] = link;
    return Task.CompletedTask;
  }

  public Task<IReadOnlyList<AgentHumanLink>> ListByPrincipalAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    IReadOnlyList<AgentHumanLink> matches =
    [
      .. Links.Values
        .Where(link => link.AgentPrincipalId == principalId.Value || link.HumanPrincipalId == principalId.Value)
        .OrderByDescending(link => link.CreatedAt)
    ];
    return Task.FromResult(matches);
  }
}
