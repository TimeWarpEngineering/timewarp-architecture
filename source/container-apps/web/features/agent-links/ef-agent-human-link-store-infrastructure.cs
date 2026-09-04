#region Purpose
// EF Core IAgentHumanLinkStore: durable AgentHumanLink persistence behind the postgres flag.
#endregion

#region Design
// Mirrors EfProfileStore dual-mode — host-owned, application stays free of PostgresDbContext.
// Find is AsNoTracking; Update loads a tracked row and copies Approve/Deny state.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Infrastructure;

using Microsoft.EntityFrameworkCore;
using TimeWarp.Architecture.Features.AgentLinks.Application;
using TimeWarp.Architecture.Features.AgentLinks.Domain;
using TimeWarp.Architecture.Persistence;
using TimeWarp.Identity;

/// <summary>Postgres-backed AgentHumanLink store.</summary>
public sealed class EfAgentHumanLinkStore : IAgentHumanLinkStore
{
  private readonly PostgresDbContext Db;

  public EfAgentHumanLinkStore(PostgresDbContext db)
  {
    Db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public async Task<AgentHumanLink?> FindAsync(AgentHumanLinkId id, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.AgentHumanLinks.AsNoTracking()
      .FirstOrDefaultAsync(link => link.Id == id, cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<AgentHumanLink?> FindOpenAsync(
    PrincipalId agentPrincipalId,
    PrincipalId humanPrincipalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    return await Db.AgentHumanLinks.AsNoTracking()
      .FirstOrDefaultAsync(
        link =>
          link.AgentPrincipalId == agentPrincipalId.Value
          && link.HumanPrincipalId == humanPrincipalId.Value
          && (link.Status == AgentHumanLinkStatus.Pending || link.Status == AgentHumanLinkStatus.Approved),
        cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task AddAsync(AgentHumanLink link, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);
    cancellationToken.ThrowIfCancellationRequested();
    Db.AgentHumanLinks.Add(link);
    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task UpdateAsync(AgentHumanLink link, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(link);
    cancellationToken.ThrowIfCancellationRequested();

    AgentHumanLink? stored = await Db.AgentHumanLinks
      .FirstOrDefaultAsync(row => row.Id == link.Id, cancellationToken)
      .ConfigureAwait(false);
    if (stored is null)
    {
      throw new InvalidOperationException($"AgentHumanLink '{link.Id}' does not exist.");
    }

    if (stored.Status == AgentHumanLinkStatus.Pending && link.Status == AgentHumanLinkStatus.Approved)
    {
      stored.Approve();
    }
    else if (stored.Status == AgentHumanLinkStatus.Pending && link.Status == AgentHumanLinkStatus.Denied)
    {
      stored.Deny();
    }

    await Db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<AgentHumanLink>> ListByPrincipalAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    List<AgentHumanLink> matches = await Db.AgentHumanLinks.AsNoTracking()
      .Where(link => link.AgentPrincipalId == principalId.Value || link.HumanPrincipalId == principalId.Value)
      .OrderByDescending(link => link.CreatedAt)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
    return matches;
  }
}
