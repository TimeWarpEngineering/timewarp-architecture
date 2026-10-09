#region Purpose
// Tri-state for the Ask surface: the probe has not finished, a model is configured, or it is not.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Whether Ask can call a model.</summary>
public enum CatalogAgentReadiness
{
  /// <summary>The configuration probe has not finished.</summary>
  Unknown = 0,

  /// <summary>The server can call a model.</summary>
  Configured = 1,

  /// <summary>No key (or the probe failed). Ask stays visible and shows the setup command.</summary>
  NotConfigured = 2
}
