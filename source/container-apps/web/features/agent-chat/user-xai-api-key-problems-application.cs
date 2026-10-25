#region Purpose
// Problem details for a user's xAI API key. Details never include the key.
#endregion

#region Design
// ByokOff is 409: the site mode is Off, so a save would store a key Ask will not use. Rejected
// repeats the length bound. ProviderRejected and ProviderUnreachable come from the probe; their
// details are scrubbed before they are built.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using TimeWarp.Architecture.Features.AgentChats;

/// <summary>Problems for a user's set, clear, and validation. None of them contain the key.</summary>
public static class UserXaiApiKeyProblems
{
  /// <summary>The site mode is Off.</summary>
  public static SharedProblemDetails ByokOff() => new()
  {
    Title = "Bring your own key is off",
    Status = 409,
    Detail = "This site does not accept a personal xAI API key."
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

  /// <summary>xAI refused the key. <paramref name="detail"/> must already be scrubbed.</summary>
  public static SharedProblemDetails ProviderRejected(string detail) => new()
  {
    Title = "xAI rejected the API key",
    Status = 400,
    Detail = detail
  };

  /// <summary>xAI did not answer. Nothing was saved.</summary>
  public static SharedProblemDetails ProviderUnreachable() => new()
  {
    Title = "xAI could not be reached",
    Status = 503,
    Detail = "xAI could not be reached. Nothing was saved."
  };
}
