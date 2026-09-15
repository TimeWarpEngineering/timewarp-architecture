#region Purpose
// Host-free LocalReturnUrl tests: post-Entra redirect stays a local path when PublicOrigin differs.
#endregion

#region Design
// PublicOrigin overrides OIDC redirect_uri only. Sanitize must still accept /Profile and must
// refuse an absolute public origin so the ticket completion cannot open-redirect.
#endregion

namespace EntraLocalReturnUrl_;

using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;

public class Sanitize_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Sanitize_Should>();

  public static Task Accept_Local_Profile_Path()
  {
    LocalReturnUrl.Sanitize("/Profile").ShouldBe("/Profile");
    return Task.CompletedTask;
  }

  public static Task Accept_Local_Path_With_Query()
  {
    LocalReturnUrl.Sanitize("/Profile?tab=security").ShouldBe("/Profile?tab=security");
    return Task.CompletedTask;
  }

  public static Task Reject_Absolute_Public_Origin()
  {
    LocalReturnUrl.Sanitize("https://arch.timewarp.work/Profile").ShouldBe("/");
    return Task.CompletedTask;
  }

  public static Task Reject_Challenge_Path()
  {
    LocalReturnUrl.Sanitize(ChallengeEntra.Path).ShouldBe("/");
    return Task.CompletedTask;
  }
}
