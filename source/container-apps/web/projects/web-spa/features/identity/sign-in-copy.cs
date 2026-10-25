#region Purpose
// Login-card copy for the sign-in methods the server offers.
#endregion

#region Design
// UI strings live in the SPA. The server owns which methods are offered; these sentences do not
// select a method. No product or client name.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

/// <summary>Sentences the login card shows around the offered sign-in methods.</summary>
public static class SignInCopy
{
  public const string WorkAccountLead = "Use your Microsoft 365 work account.";

  public const string Microsoft365UnavailableMessage = "Microsoft 365 sign-in is not available.";
}
