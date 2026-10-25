#region Purpose
// Sentences Ask shows when no xAI key resolves, chosen by the site BYOK mode.
#endregion

#region Design
// Required names the user's Profile key only, because a site key would not be used. Allow names
// both, because either key works. Off tells the reader to ask an admin and keeps the host setup
// text, because that mode is the pre-BYOK path. None of the sentences include a key or the
// "Admin → AI" label; that label stays on AdminHint and is omitted when the mode is Required.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using TimeWarp.Identity;

/// <summary>Ask copy for a missing key. The key is not a parameter.</summary>
public static class XaiByokCopy
{
  /// <summary>Required, and Allow's user half.</summary>
  public const string SetYourKey = "Set your xAI API key on Profile.";

  /// <summary>Allow, when neither the user nor the site has a key.</summary>
  public const string SetYourKeyOrAskAdmin =
    "Set your xAI API key on Profile, or ask an admin to set the site key.";

  /// <summary>Off. The host setup text follows it.</summary>
  public const string AskAdmin = "Ask an admin to set the site xAI API key.";

  /// <summary>The 503 detail and the Ask hint for <paramref name="mode"/>.</summary>
  public static string MissingKeyDetail(XaiByokMode mode, bool development) =>
    mode switch
    {
      XaiByokMode.Required => SetYourKey,
      XaiByokMode.Allow => SetYourKeyOrAskAdmin,
      _ => AskAdmin + " " + XaiChatDefaults.HostSetupText(development)
    };
}
