#region Purpose
// Server-side handler for ListPrincipals: principal store + effective roles + gated profile fields.
#endregion

#region Design
// Lists all principals from IPrincipalStore (CreatedAt order from the port) and attaches
// effective RoleIds via IEffectiveRolesResolver so the SPA multi-select matches RequireRole.
// Task 328: the caller's admin.profiles.read is checked through IPermissionEvaluator under the
// identity-session scheme. Both endpoint schemes (identity-session, mock-identity-session) are
// human session schemes that expand roles identically, so one scheme name gives the same answer.
// Without the permission, DisplayName and Email stay null (no PII), and a Search or a sort by
// DisplayName/Email is 403 (PrincipalProblems.ProfileAccessRequired) instead of leaking through
// ordering. With it, profiles come from IProfileDirectory (platform port, TWA0009) in one batch;
// Search asks the directory for matching principal ids and filters to them.
// Sorting is in memory over the whole list (no paging yet): CreatedAt keeps store order
// (reversed when descending); DisplayName/Email compare OrdinalIgnoreCase with nulls last in
// both directions; Kind compares by enum name. Every sort breaks ties by store order.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals.Application;

using TimeWarp.Architecture.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Admin.Principals.ListPrincipals;

public sealed partial class ListPrincipals
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IEffectiveRolesResolver EffectiveRolesResolver;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IPermissionEvaluator PermissionEvaluator;
    private readonly IProfileDirectory ProfileDirectory;

    public Handler(
      IPrincipalStore principalStore,
      IEffectiveRolesResolver effectiveRolesResolver,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IPermissionEvaluator permissionEvaluator,
      IProfileDirectory profileDirectory)
    {
      PrincipalStore = principalStore;
      EffectiveRolesResolver = effectiveRolesResolver;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      PermissionEvaluator = permissionEvaluator;
      ProfileDirectory = profileDirectory;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      bool canReadProfiles = await CanReadProfilesAsync(cancellationToken).ConfigureAwait(false);
      string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
      bool needsProfiles = search is not null
        || query.SortBy is PrincipalListSortColumn.DisplayName or PrincipalListSortColumn.Email;
      if (!canReadProfiles && needsProfiles)
      {
        return PrincipalProblems.ProfileAccessRequired();
      }

      IReadOnlyList<Principal> principals = await PrincipalStore
        .ListPrincipalsAsync(cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyDictionary<Guid, ProfileDirectoryEntry> profiles = new Dictionary<Guid, ProfileDirectoryEntry>();
      if (canReadProfiles)
      {
        profiles = await ProfileDirectory
          .FindManyAsync([.. principals.Select(static principal => principal.Id.Value)], cancellationToken)
          .ConfigureAwait(false);

        if (search is not null)
        {
          IReadOnlyList<Guid> matches = await ProfileDirectory
            .SearchAsync(search, cancellationToken)
            .ConfigureAwait(false);
          HashSet<Guid> matching = [.. matches];
          principals = [.. principals.Where(principal => matching.Contains(principal.Id.Value))];
        }
      }

      principals = Sort(principals, profiles, query.SortBy, query.SortDescending);

      var items = new PrincipalSummaryDto[principals.Count];
      for (int i = 0; i < principals.Count; i++)
      {
        Principal principal = principals[i];
        IReadOnlyList<Guid> roles = await EffectiveRolesResolver
          .GetEffectiveRoleIdsAsync(principal.Id, cancellationToken)
          .ConfigureAwait(false);

        profiles.TryGetValue(principal.Id.Value, out ProfileDirectoryEntry? profile);
        items[i] = new PrincipalSummaryDto(
          principal.Id,
          principal.Kind,
          principal.TrustTier,
          principal.IsActive,
          principal.IsQuarantined,
          [.. roles],
          profile?.DisplayName,
          profile?.Email);
      }

      return new Response(items.Length, items);
    }

    private async Task<bool> CanReadProfilesAsync(CancellationToken cancellationToken)
    {
      PrincipalId? caller = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (caller is null)
      {
        return false;
      }

      return await PermissionEvaluator
        .HasPermissionAsync(
          caller.Value,
          AuthenticationSchemeNames.IdentitySession,
          PermissionIds.AdminProfilesRead,
          cancellationToken)
        .ConfigureAwait(false);
    }

    private static IReadOnlyList<Principal> Sort(
      IReadOnlyList<Principal> principals,
      IReadOnlyDictionary<Guid, ProfileDirectoryEntry> profiles,
      PrincipalListSortColumn sortBy,
      bool descending)
    {
      if (sortBy == PrincipalListSortColumn.CreatedAt)
      {
        return descending ? [.. principals.Reverse()] : principals;
      }

      // Stable ordering over the store order keeps ties deterministic.
      IEnumerable<Principal> ordered;
      if (sortBy == PrincipalListSortColumn.Kind)
      {
        ordered = descending
          ? principals.OrderByDescending(static principal => principal.Kind.ToString(), StringComparer.Ordinal)
          : principals.OrderBy(static principal => principal.Kind.ToString(), StringComparer.Ordinal);
        return [.. ordered];
      }

      string? Key(Principal principal)
      {
        if (!profiles.TryGetValue(principal.Id.Value, out ProfileDirectoryEntry? profile))
        {
          return null;
        }

        return sortBy == PrincipalListSortColumn.DisplayName ? profile.DisplayName : profile.Email;
      }

      // Nulls last in both directions: split first, then order the keyed rows.
      List<Principal> keyed = [.. principals.Where(principal => Key(principal) is not null)];
      List<Principal> missing = [.. principals.Where(principal => Key(principal) is null)];
      ordered = descending
        ? keyed.OrderByDescending(Key, StringComparer.OrdinalIgnoreCase)
        : keyed.OrderBy(Key, StringComparer.OrdinalIgnoreCase);
      return [.. ordered, .. missing];
    }
  }
}
