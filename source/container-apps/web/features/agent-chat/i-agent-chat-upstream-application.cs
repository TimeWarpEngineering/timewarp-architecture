#region Purpose
// Server port the completion handler calls. The application layer does not reference a model SDK.
#endregion

#region Design
// IsConfigured is cheap and does not open a connection. CompleteAsync either returns the model's
// text and function calls or a problem whose detail is safe to show. Implementations must not
// execute tool declarations.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

/// <summary>Forwards one completion to the configured model, or reports that none is configured.</summary>
public interface IAgentChatUpstream
{
  /// <summary>True when a real key is set, or the Development/Testing fake is explicitly on.</summary>
  bool IsConfigured { get; }

  /// <summary>Model id to show in the status response. Empty when nothing is configured.</summary>
  string? Model { get; }

  /// <summary>Forwards the turn. Does not invoke tools.</summary>
  Task<OneOf<Response, SharedProblemDetails>> CompleteAsync
  (
    Command command,
    CancellationToken cancellationToken
  );
}
