#region Purpose
// Server port the completion handler calls. The application layer does not reference a model SDK.
#endregion

#region Design
// IsConfiguredAsync is the fake flag, the host key, or one read of the admin secret. BeginKeyResolution
// opens a slot on the caller's async flow; the server upstream fills it from that read and CompleteAsync
// consumes it, so one turn does not read twice. A value set inside IsConfiguredAsync would not be
// visible to the caller. A changed admin key is visible on the next call. CompleteAsync either
// returns the model's text and function calls or a problem whose detail is safe to show.
// Implementations must not execute tool declarations. Model is the sync host-or-fake view; the
// configuration handler computes the model from options once IsConfiguredAsync is true, so an
// admin-only key still reports one.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>Forwards one completion to the configured model, or reports that none is configured.</summary>
public interface IAgentChatUpstream
{
  /// <summary>
  /// Opens a one-read slot for the <see cref="IsConfiguredAsync"/> and <see cref="CompleteAsync"/>
  /// that follow on this async flow. The server upstream fills the slot; other implementations do nothing.
  /// </summary>
  void BeginKeyResolution()
  {
  }

  /// <summary>
  /// True when the Development/Testing fake is on, the host key is set, or an admin key can be read.
  /// </summary>
  Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

  /// <summary>Model id to show in the status response. Empty when nothing is configured.</summary>
  string? Model { get; }

  /// <summary>Forwards the turn. Does not invoke tools.</summary>
  Task<OneOf<Response, SharedProblemDetails>> CompleteAsync
  (
    Command command,
    CancellationToken cancellationToken
  );

  /// <summary>Mode from the last resolution on this async flow. Allow when nothing has resolved.</summary>
  XaiByokMode ResolvedMode => XaiByokMode.Allow;

  /// <summary>
  /// Sentence for a missing key. The default is the host setup text so a stub that does not
  /// know the policy keeps the pre-BYOK sentence.
  /// </summary>
  string MissingKeyDetail(bool development) => XaiChatDefaults.HostSetupText(development);
}
