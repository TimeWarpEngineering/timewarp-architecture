#region Purpose
// Problem details for admin writes of the site xAI API key. Details never include the key.
#endregion

#region Design
// HostManaged is 409: the host value wins and the stored row must not change. StorageUnavailable
// is 503 when this host has no durable store. Its detail is the host setup text and does not say
// to open Admin → AI. StorageFailed is 503 when the store throws (missing table or the database
// is down), matching site settings. Rejected is the same bound as the contract validator, repeated
// so a caller that skips the mediator still cannot store a key whose suffix is the secret.
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

  /// <summary>This host has nowhere durable to store the key. The detail does not mention Admin → AI.</summary>
  public static SharedProblemDetails StorageUnavailable(bool development) => new()
  {
    Title = "Host configuration only",
    Status = 503,
    Detail = XaiChatDefaults.HostSetupText(development)
  };

  /// <summary>The store threw. Retry after migrations, or when the database is back.</summary>
  public static SharedProblemDetails StorageFailed() => new()
  {
    Title = "xAI key storage unavailable",
    Status = 503,
    Detail = "Protected secret storage is unavailable. Retry after database migrations finish."
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
