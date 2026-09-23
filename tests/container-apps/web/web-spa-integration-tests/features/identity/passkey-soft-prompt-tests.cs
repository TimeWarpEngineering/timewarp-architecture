#region Purpose
// RFC 219 D8: Entra-without-passkey soft prompt visibility and non-gate route proofs.
#endregion

#region Design
// Pure-function + generated page Policy accessors — no SPA host. Visibility is the
// GetCredentials Type-list predicate. Route proofs pin Home as Anonymous and Profile/Settings
// as permission policies, never a passkey requirement. Dismiss is a boolean the predicate
// honors; it cannot rewrite [Page] Policy.
#endregion

namespace PasskeySoftPrompt_;

using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Profiles;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

[TestTag("Unit")]
public class ShouldShow_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ShouldShow_Given_>();

  public static Task Entra_only_principal_Should_see_prompt()
  {
    PasskeySoftPrompt.ShouldShow([Entra()], dismissed: false).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Principal_with_passkey_Should_not_see_prompt()
  {
    PasskeySoftPrompt.ShouldShow([Entra(), Passkey()], dismissed: false).ShouldBeFalse();
    PasskeySoftPrompt.ShouldShow([Passkey()], dismissed: false).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Dismissed_entra_only_Should_hide_prompt()
  {
    PasskeySoftPrompt.ShouldShow([Entra()], dismissed: true).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Null_or_empty_snapshot_Should_hide_prompt()
  {
    PasskeySoftPrompt.ShouldShow(null, dismissed: false).ShouldBeFalse();
    PasskeySoftPrompt.ShouldShow([], dismissed: false).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Revoked_entra_without_passkey_Should_hide_prompt()
  {
    PasskeySoftPrompt.ShouldShow([Entra(isActive: false)], dismissed: false).ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Agent_key_does_not_count_as_passkey()
  {
    PasskeySoftPrompt.ShouldShow([Entra(), AgentKey()], dismissed: false).ShouldBeTrue();
    return Task.CompletedTask;
  }

  private static CredentialSummary Entra(bool isActive = true) =>
    Summary(CredentialType.EntraAccount, isActive);

  private static CredentialSummary Passkey() =>
    Summary(CredentialType.Passkey, isActive: true);

  private static CredentialSummary AgentKey() =>
    Summary(CredentialType.AgentKey, isActive: true);

  private static CredentialSummary Summary(CredentialType type, bool isActive) =>
    new
    (
      CredentialId.New(),
      type,
      label: type.ToString(),
      nickname: null,
      DateTimeOffset.UtcNow,
      revokedAt: isActive ? null : DateTimeOffset.UtcNow,
      isActive,
      RegisteredWith.Unknown,
      "0123abcd"
    );
}

[TestTag("Unit")]
public class Dismiss_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Dismiss_Should_>();

  public static Task Not_block_home_profile_or_settings_routes()
  {
    HomePage.GetPageUrl().ShouldBe("/");
    HomePage.Policy.ShouldBe(AuthorizationConstants.Policies.Anonymous);

    ProfilePage.GetPageUrl().ShouldBe("/Profile");
    ProfilePage.Policy.ShouldBe(PermissionIds.ProfileRead);

    SettingsPage.GetPageUrl().ShouldBe("/Settings");
    SettingsPage.Policy.ShouldBe(PermissionIds.SettingsRead);

    HomePage.Policy.ShouldNotContain("passkey", Case.Insensitive);
    ProfilePage.Policy.ShouldNotContain("passkey", Case.Insensitive);
    SettingsPage.Policy.ShouldNotContain("passkey", Case.Insensitive);
    return Task.CompletedTask;
  }

  public static Task Later_storage_key_is_stable()
  {
    PasskeySoftPrompt.LaterStorageKey.ShouldBe("twe-passkey-soft-prompt-later");
    return Task.CompletedTask;
  }
}
