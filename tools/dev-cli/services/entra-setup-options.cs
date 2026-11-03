#region Purpose
// Parsed `dev entra setup` options handed from the endpoint to the setup runner.
#endregion

#region Design
// A plain record so the endpoint stays a thin Nuru shell and the steps do not see the command type.
#endregion

namespace DevCli.Services;

internal sealed record EntraSetupOptions(
  string? Name,
  string? Tenant,
  string? PublicOrigin,
  IReadOnlyList<string> RedirectUri,
  bool NewSecret,
  bool DryRun);
