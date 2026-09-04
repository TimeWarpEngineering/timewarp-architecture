#region Purpose
// Application port for AgentHumanLink persistence — handlers never take DbContext.
#endregion

#region Design
// Dual-mode like IProfileStore: in-memory singleton default, EfAgentHumanLinkStore scoped when
// PostgresDbModule sees a connection string. FindOpen is the uniqueness check for a live
// (Pending or Approved) pair so a denied link can be requested again.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Identity;

/// <summary>Durable AgentHumanLink lookup, insert, and update.</summary>
public interface IAgentHumanLinkStore
{
  Task<AgentHumanLink?> FindAsync(AgentHumanLinkId id, CancellationToken cancellationToken = default);

  /// <summary>The live (Pending or Approved) link for this pair, if any.</summary>
  Task<AgentHumanLink?> FindOpenAsync(
    PrincipalId agentPrincipalId,
    PrincipalId humanPrincipalId,
    CancellationToken cancellationToken = default);

  Task AddAsync(AgentHumanLink link, CancellationToken cancellationToken = default);

  Task UpdateAsync(AgentHumanLink link, CancellationToken cancellationToken = default);

  /// <summary>Links where the principal is the agent or the human, newest first.</summary>
  Task<IReadOnlyList<AgentHumanLink>> ListByPrincipalAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default);
}
