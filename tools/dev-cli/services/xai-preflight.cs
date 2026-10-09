#region Purpose
// Decides whether `dev run` should warn that the xAI key is missing. Never prints the key.
#endregion

#region Design
// dev-cli does not reference web-server. The key name, the project path and the user-secrets id
// duplicate XaiChatDefaults (source/container-apps/web/features/agent-chat/xai-chat-contracts.cs).
// A key in the environment (XAI__ApiKey) or in `dotnet user-secrets list` output counts.
// That list prints `Key = Value` (spaces around the separator), the same shape
// AspireDeploy.ParseUserSecret reads. The value is never returned.
// SetupCommand must stay identical to XaiChatDefaults.SetupCommand.
#endregion

namespace DevCli.Services;

internal static class XaiPreflight
{
  internal const string ApiKeyName = "XAI:ApiKey";

  internal const string EnvironmentVariable = "XAI__ApiKey";

  internal const string WebServerProject =
    "source/container-apps/web/projects/web-server/web-server.csproj";

  // web-server's <UserSecretsId>. --id makes the setup command work from any directory (task 290).
  internal const string UserSecretsId = "0e53fdd3-6f93-4d5a-9c86-040621f7929e";

  internal const string SetupCommand =
    "dotnet user-secrets set \"XAI:ApiKey\" \"<your-xai-key>\" --id " + UserSecretsId;

  internal const string MissingKeyLead =
    "Warning: XAI:ApiKey is not set. Ctrl-K Ask will show this command:";

  internal const string UnreadableSecretsLead =
    "Warning: could not read web-server user secrets, so XAI:ApiKey is treated as unset. Ctrl-K Ask will show this command:";

  internal static bool HasKey(string? environmentValue, string? secretsList) =>
    !string.IsNullOrWhiteSpace(environmentValue) || HasSecret(secretsList);

  internal static string WarningLead(bool secretsListSucceeded) =>
    secretsListSucceeded ? MissingKeyLead : UnreadableSecretsLead;

  private static bool HasSecret(string? secretsList)
  {
    if (string.IsNullOrWhiteSpace(secretsList))
    {
      return false;
    }

    foreach (string line in secretsList.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      int separator = line.IndexOf(" = ", StringComparison.Ordinal);
      if (separator <= 0)
      {
        continue;
      }

      if (!string.Equals(line[..separator].Trim(), ApiKeyName, StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      return line[(separator + 3)..].Trim().Length > 0;
    }

    return false;
  }
}
