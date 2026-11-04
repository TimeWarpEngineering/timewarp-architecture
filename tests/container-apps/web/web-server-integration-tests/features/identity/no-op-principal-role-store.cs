#region Purpose
// IPrincipalRoleStore stand-in that stores nothing, for EntraTicketProcessor tests that ignore roles.
#endregion

namespace EntraTicketProcessor_;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;

internal sealed class NoOpPrincipalRoleStore : IPrincipalRoleStore
{
  public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default) =>
    Task.FromResult<IReadOnlyList<Guid>>([]);

  public Task SetRoleIdsAsync(
    PrincipalId principalId,
    IReadOnlyList<Guid> roleIds,
    CancellationToken cancellationToken = default) =>
    Task.CompletedTask;

  public Task<bool> TryClaimFirstAdministratorAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default) =>
    Task.FromResult(false);
}
