#region Purpose
// Implements the principal effective-roles algorithm against the role store + bootstrap options.
#endregion

#region Design
// See IEffectiveRolesResolver Design for the SSOT algorithm. Bootstrap ids are parsed once on
// first resolve (lazy) from BootstrapAdministratorOptions string[]; invalid Guids are ignored
// so a typo does not crash the host — ValidateOnStart is intentionally not used so empty
// Development config stays valid. Ordering follows RoleIds.All so UI and claims stay stable.
// Task 160: IPrincipalRoleStore.GetRoleIdsAsync failures wrap as RoleResolutionFailedException
// (never empty-roles). Cancellation is not wrapped. RoleResolutionFailedException is rethrown
// as-is so a store that already throws the typed failure is not double-wrapped.
#endregion

namespace TimeWarp.Architecture.Features;

using Microsoft.Extensions.Options;
using TimeWarp.Identity;

/// <summary>SSOT effective-role resolution for passkey principals.</summary>
public sealed class EffectiveRolesResolver : IEffectiveRolesResolver
{
  private readonly IPrincipalRoleStore RoleStore;
  private readonly IOptions<BootstrapAdministratorOptions> BootstrapOptions;
  private readonly Lazy<HashSet<PrincipalId>> BootstrapPrincipalIds;

  public EffectiveRolesResolver(
    IPrincipalRoleStore roleStore,
    IOptions<BootstrapAdministratorOptions> bootstrapOptions)
  {
    RoleStore = roleStore;
    BootstrapOptions = bootstrapOptions;
    BootstrapPrincipalIds = new Lazy<HashSet<PrincipalId>>(ParseBootstrapIds);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<Guid>> GetEffectiveRoleIdsAsync(
    PrincipalId principalId,
    CancellationToken cancellationToken = default)
  {
    IReadOnlyList<Guid> stored;
    try
    {
      stored = await RoleStore.GetRoleIdsAsync(principalId, cancellationToken)
        .ConfigureAwait(false);
    }
    catch (Exception exception) when (exception is not OperationCanceledException
      and not RoleResolutionFailedException)
    {
      throw new RoleResolutionFailedException(
        "Failed to resolve effective roles from the principal role store.",
        exception);
    }

    HashSet<Guid> effective = stored.Count == 0
      ? [RoleIds.Member]
      : [.. stored];

    if (BootstrapPrincipalIds.Value.Contains(principalId))
    {
      effective.Add(RoleIds.Administrator);
      effective.Add(RoleIds.Member);
    }

    return RoleIds.All.Where(effective.Contains).ToArray();
  }

  private HashSet<PrincipalId> ParseBootstrapIds()
  {
    HashSet<PrincipalId> set = [];
    foreach (string raw in BootstrapOptions.Value.BootstrapAdministratorPrincipalIds ?? [])
    {
      if (Guid.TryParse(raw, out Guid guid) && guid != Guid.Empty)
      {
        set.Add(PrincipalId.From(guid));
      }
    }

    return set;
  }
}
