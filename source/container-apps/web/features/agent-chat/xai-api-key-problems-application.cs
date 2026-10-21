#region Purpose
// Problem details for admin writes of the site xAI API key. Details never include the key.
#endregion

#region Design
// HostManaged is 409: the host value wins and the stored row must not change. StorageUnavailable
// is 503 and carries the environment's setup hint, which is how Admin → AI explains a host-only
// deployment. Rejected is the same bound as the contract validator, repeated so a caller that
// skips the mediator still cannot store a key whose suffix is the secret.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using TimeWarp.Architecture.Features.AgentChats;

/// <summary>Problems for set and clear. None of them contain the key.</summary>
public static class XaiApiKeyProblems
{
  /// <summary>No authenticated principal.</summary>
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };

  /// <summary><c>XAI:ApiKey</c> is set. Admin cannot change the stored key.</summary>
  public static SharedProblemDetails HostManaged() => new()
  {
    Title = "Managed by the host",
    Status = 409,
    Detail = "XAI:ApiKey is set on the host. Admin cannot change it."
  };

  /// <summary>This host has nowhere durable to store the key.</summary>
  public static SharedProblemDetails StorageUnavailable(bool development) => new()
  {
    Title = "Host configuration only",
    Status = 503,
    Detail = XaiChatDefaults.SetupHint(development)
  };

  /// <summary>The key failed the length or whitespace bound.</summary>
  public static SharedProblemDetails Rejected() => new()
  {
    Title = "Invalid xAI API key",
    Status = 400,
    Detail = "Enter the xAI API key ("
      + SiteXaiApiKeyNames.MinimumLength
      + " to "
      + SiteXaiApiKeyNames.MaximumLength
      + " characters, no spaces)."
  };
}
