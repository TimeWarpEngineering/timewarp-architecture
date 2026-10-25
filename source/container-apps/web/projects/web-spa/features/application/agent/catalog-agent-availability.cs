#region Purpose
// Reads the Ask probe on AgentSurfaceState. The button is not gated on IChatClient registration.
#endregion

#region Design
// Task 271 treated "an IChatClient is registered" as configured, which hid Ask in the template.
// Task 289 always registers a relay. Configured means the server status probe said so. Until
// that probe finishes the readiness is Unknown. Task 293: only a server answer of "no key" is
// NotConfigured and shows that environment's setup hint. A 401 is Unauthenticated and any other
// failure is Error with the problem text (ChatReadinessProbe). Ask stays visible in every state.
// WebMCP does not consult this.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Ask readiness for the palette and the modal.</summary>
public static class CatalogAgentAvailability
{
  /// <summary>Probe result. Unknown until <c>LoadChatConfiguration</c> finishes.</summary>
  public static CatalogAgentReadiness Readiness(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return state.ChatReadiness;
  }

  /// <summary>Host setup text from the probe, or the deployed hint when the probe has not stored one.</summary>
  public static string SetupCommand(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return string.IsNullOrWhiteSpace(state.ChatSetupCommand)
      ? XaiChatDefaults.DeployedSetupHint
      : state.ChatSetupCommand;
  }

  /// <summary>Admin → AI sentence from the probe, or null when this host cannot store a key.</summary>
  public static string? AdminHint(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return string.IsNullOrWhiteSpace(state.ChatAdminHint) ? null : state.ChatAdminHint;
  }

  /// <summary>Whose key to set when Ask is not configured, or null when a key resolved.</summary>
  public static string? KeyHint(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return string.IsNullOrWhiteSpace(state.ChatKeyHint) ? null : state.ChatKeyHint;
  }

  /// <summary>Status, title and detail of a failed probe; null unless readiness is Error.</summary>
  public static string? Problem(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return state.ChatReadiness == CatalogAgentReadiness.Error ? state.ChatProblem : null;
  }
}
