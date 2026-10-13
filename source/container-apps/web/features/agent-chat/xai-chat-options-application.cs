#region Purpose
// Bound XAI configuration. The API key is optional so a missing secret never fails startup.
#endregion

#region Design
// Empty Model and Endpoint fall back to XaiChatDefaults inside the upstream. UseFakeUpstream
// is honored only in Development and Testing, and only when set. It is not in appsettings.
// A developer who turns the flag on in Development replaces the real Grok client.
// RecordChats, PrivacyNotice, and PrivacyUrl are the per-app chat-recording notice (task 347).
// They default to XaiChatDefaults, so an app that sets nothing records nothing and shows nothing.
// StarterPrompts is the Ask empty-state list (task 348). It is get-only so configuration binding
// fills it; empty means AgentChatStarterPrompts.Defaults.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

/// <summary>xAI relay options. <see cref="ApiKey"/> stays null when the secret is absent.</summary>
public sealed class XaiChatOptions
{
  /// <summary>Configuration section name.</summary>
  public const string SectionName = XaiChatDefaults.SectionName;

  /// <summary>xAI API key from user-secrets or the environment. Never from appsettings.</summary>
  public string? ApiKey { get; set; }

  /// <summary>Model id. Empty uses <see cref="XaiChatDefaults.DefaultModel"/>.</summary>
  public string Model { get; set; } = XaiChatDefaults.DefaultModel;

  /// <summary>OpenAI-compatible endpoint. Empty uses <see cref="XaiChatDefaults.DefaultEndpoint"/>.</summary>
  public string Endpoint { get; set; } = XaiChatDefaults.DefaultEndpoint;

  /// <summary>
  /// When true in Development or Testing, complete calls use the in-process fake and ignore a real key.
  /// Ignored in every other environment.
  /// </summary>
  public bool UseFakeUpstream { get; set; }

  /// <summary>True when this deployment records chats. Ask shows the notice only when true.</summary>
  public bool RecordChats { get; set; } = XaiChatDefaults.RecordChats;

  /// <summary>Notice text. Blank uses <see cref="XaiChatDefaults.PrivacyNotice"/>.</summary>
  public string? PrivacyNotice { get; set; } = XaiChatDefaults.PrivacyNotice;

  /// <summary>Optional privacy page linked from the notice. Blank shows no link.</summary>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "PrivacyUrl is a relative app path or an absolute href, bound from configuration.")]
  public string? PrivacyUrl { get; set; } = XaiChatDefaults.PrivacyUrl;

  /// <summary>Ask empty-state starter prompts (XAI:StarterPrompts). Empty uses the template defaults.</summary>
  public IList<AgentChatStarterPrompt> StarterPrompts { get; } = [];
}
