#region Purpose
// Default server URL and key-file path for agent-identity CLI commands.
#endregion
#region Design
// Server default is web-server launchSettings HTTPS (63611), NOT the YARP port —
// agents talk to the identity host that implements the ceremony endpoints.
// Key material lives under XDG-ish ~/.config/timewarp/agent-identity/ so it is
// out of the repo tree and never committed by accident.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class PathDefaults
{
  public const string DefaultServer = "https://localhost:63611";

  public string DefaultKeyFilePath { get; } = ResolveDefaultKeyFilePath();

  public static string ResolveStorePath(string keyFilePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(keyFilePath);
    string full = Path.GetFullPath(keyFilePath);
    string? directory = Path.GetDirectoryName(full);
    string fileName = Path.GetFileNameWithoutExtension(full);
    return Path.Combine(directory ?? string.Empty, $"{fileName}.store.json");
  }

  private static string ResolveDefaultKeyFilePath()
  {
    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return Path.Combine(home, ".config", "timewarp", "agent-identity", "default.pem");
  }
}
