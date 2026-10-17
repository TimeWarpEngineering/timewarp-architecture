#region Purpose
// Resolves feedback owners' display names for admin views, gated on admin.profiles.read.
#endregion

#region Design
// Task 336. Feedback reaches profiles only through the platform port IProfileDirectory
// (TWA0009), in one FindManyAsync batch over the distinct owner ids of a page or an item;
// never per row. The caller's admin.profiles.read is checked through IPermissionEvaluator
// under the scheme that authenticated this request (task 378 review M1).
// IAgentPermissionScopeSource returns the token scopes when the caller is that agent
// principal, and null for a human session. An agent-token caller is evaluated on the
// agent-token scheme only, so a human role that holds admin.profiles.read does not name
// owners for a feedback:read token. A human session is evaluated on the identity-session
// and mock-identity-session schemes, which expand roles the same way. Without the
// permission (or without a caller) the directory is not asked at all and the result is
// empty, so no PII is read or returned. A missing profile row is simply absent from the
// result. Only the display name is exposed; the email stays on the principal detail page
// (task 328), which the owner cell links to. Scoped: one instance per request, and the
// permission answer is cached for that request.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features;

public sealed class FeedbackOwnerNames
{
  private static readonly IReadOnlyDictionary<Guid, string> None = new Dictionary<Guid, string>();

  private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
  private readonly IPermissionEvaluator PermissionEvaluator;
  private readonly IProfileDirectory ProfileDirectory;
  private readonly IAgentPermissionScopeSource AgentPermissionScopeSource;
  private bool? CanReadProfiles;

  public FeedbackOwnerNames(
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IPermissionEvaluator permissionEvaluator,
    IProfileDirectory profileDirectory,
    IAgentPermissionScopeSource agentPermissionScopeSource)
  {
    CurrentPrincipalAccessor = currentPrincipalAccessor;
    PermissionEvaluator = permissionEvaluator;
    ProfileDirectory = profileDirectory;
    AgentPermissionScopeSource = agentPermissionScopeSource;
  }

  /// <summary>Display names keyed by owner id; empty without admin.profiles.read.</summary>
  public async Task<IReadOnlyDictionary<Guid, string>> FindManyAsync(
    IEnumerable<Guid> ownerPrincipalIds,
    CancellationToken cancellationToken)
  {
    Guid[] distinct = [.. ownerPrincipalIds.Where(static id => id != Guid.Empty).Distinct()];
    if (distinct.Length == 0 || !await CanReadProfilesAsync(cancellationToken).ConfigureAwait(false))
    {
      return None;
    }

    IReadOnlyDictionary<Guid, ProfileDirectoryEntry> profiles = await ProfileDirectory
      .FindManyAsync(distinct, cancellationToken)
      .ConfigureAwait(false);
    return profiles.ToDictionary(static pair => pair.Key, static pair => pair.Value.DisplayName);
  }

  /// <summary>One owner's display name, or null without admin.profiles.read or a profile row.</summary>
  public async Task<string?> FindAsync(Guid ownerPrincipalId, CancellationToken cancellationToken)
  {
    IReadOnlyDictionary<Guid, string> names = await FindManyAsync([ownerPrincipalId], cancellationToken)
      .ConfigureAwait(false);
    return names.TryGetValue(ownerPrincipalId, out string? name) ? name : null;
  }

  private async Task<bool> CanReadProfilesAsync(CancellationToken cancellationToken)
  {
    if (CanReadProfiles is { } cached)
    {
      return cached;
    }

    PrincipalId? caller = await CurrentPrincipalAccessor
      .GetCurrentPrincipalIdAsync(cancellationToken)
      .ConfigureAwait(false);
    if (caller is null)
    {
      CanReadProfiles = false;
      return false;
    }

    if (AgentPermissionScopeSource.GetHeldScopesFor(caller.Value) is not null)
    {
      CanReadProfiles = await PermissionEvaluator
        .HasPermissionAsync(
          caller.Value,
          AuthenticationSchemeNames.AgentToken,
          PermissionIds.AdminProfilesRead,
          cancellationToken)
        .ConfigureAwait(false);
      return CanReadProfiles.Value;
    }

    CanReadProfiles = await PermissionEvaluator
        .HasPermissionAsync(
          caller.Value,
          AuthenticationSchemeNames.IdentitySession,
          PermissionIds.AdminProfilesRead,
          cancellationToken)
        .ConfigureAwait(false)
      || await PermissionEvaluator
        .HasPermissionAsync(
          caller.Value,
          AuthenticationSchemeNames.MockIdentitySession,
          PermissionIds.AdminProfilesRead,
          cancellationToken)
        .ConfigureAwait(false);
    return CanReadProfiles.Value;
  }
}
