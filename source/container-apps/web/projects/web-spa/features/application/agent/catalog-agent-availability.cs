#region Purpose
// Reads the Ask probe on AgentSurfaceState. The button is not gated on IChatClient registration.
#endregion

#region Design
// Task 271 treated "an IChatClient is registered" as configured, which hid Ask in the template.
// Task 289 always registers a relay. Configured means the server status probe said so. Until
// that probe finishes the readiness is Unknown. A missing key, a failed probe, or a 401 is
// NotConfigured and still shows Ask, with the pwsh user-secrets command.
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

  /// <summary>Setup command from the probe, or the constant when the probe has not stored one.</summary>
  public static string SetupCommand(AgentSurfaceState state)
  {
    ArgumentNullException.ThrowIfNull(state);
    return string.IsNullOrWhiteSpace(state.ChatSetupCommand)
      ? XaiChatDefaults.SetupCommand
      : state.ChatSetupCommand;
  }
}
