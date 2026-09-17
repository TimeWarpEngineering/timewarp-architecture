#region Purpose
// Shared az / dotnet user-secrets process runner for `dev entra` with dry-run and secret-safe capture.
#endregion

#region Design
// Amuru Shell.Builder only — never System.Diagnostics.Process. Mutation captures skip execution
// on dry-run and print the invocation. Read-only / token captures always execute so tenant
// discovery and user-secrets list work under --dry-run; token stdout and Authorization headers
// are never printed (Bearer values masked as ******** when an invocation line is shown). az on
// PATH is PathResolver, not a thrown process start.
#endregion

namespace DevCli.Services;

internal sealed class EntraCli
{
  internal const string AzLoginHint = "az login is required. Run `az login` then retry.";
  internal const string GraphOrganizationUrl =
    "https://graph.microsoft.com/v1.0/organization?$select=id,displayName,verifiedDomains";
  internal const string GraphResource = "https://graph.microsoft.com";

  private readonly ITerminal Terminal;
  private readonly string RepoRoot;
  private readonly CancellationToken Ct;

  internal EntraCli(ITerminal terminal, string repoRoot, bool dryRun, CancellationToken ct)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
    IsDryRun = dryRun;
    Ct = ct;
  }

  internal bool IsDryRun { get; }

  internal static bool TryFindRepoRoot(ITerminal terminal, out string repoRoot)
  {
    string? root = Git.FindRoot();
    if (root is null)
    {
      terminal.WriteErrorLine("Error: could not find repository root.");
      repoRoot = "";
      return false;
    }

    repoRoot = root;
    return true;
  }

  internal static bool AzIsOnPath() => PathResolver.ResolveExecutable("az") is not null;

  internal string WebServerProjectPath => Path.Combine(RepoRoot, EntraSetup.WebServerProject);

  internal Task<CommandOutput> CaptureAzAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, skipOnDryRun: true, maskArguments: false, maskAuthorizationHeader: false);

  internal Task<CommandOutput> CaptureAzSecretAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, skipOnDryRun: true, maskArguments: false, maskAuthorizationHeader: false);

  /// <summary>
  /// Always executes (including under --dry-run). Prints a dry-run invocation line when dry-run.
  /// </summary>
  internal Task<CommandOutput> CaptureAzReadOnlyAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, skipOnDryRun: false, maskArguments: false, maskAuthorizationHeader: false);

  /// <summary>
  /// Always executes Graph/az rest calls that may carry a Bearer header. Masks the header in dry-run lines.
  /// </summary>
  internal Task<CommandOutput> CaptureAzReadOnlyMaskedAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, skipOnDryRun: false, maskArguments: false, maskAuthorizationHeader: true);

  /// <summary>
  /// Always executes get-access-token. Never prints stdout (the token). Dry-run still prints the invocation.
  /// </summary>
  internal Task<CommandOutput> CaptureAzTokenAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, skipOnDryRun: false, maskArguments: false, maskAuthorizationHeader: false, printStdoutNever: true);

  internal Task<CommandOutput> CaptureDotNetAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("dotnet", arguments, skipOnDryRun: true, maskArguments: false, maskAuthorizationHeader: false);

  internal Task<CommandOutput> SetUserSecretAsync(string key, string value, bool maskValue)
  {
    string[] arguments = ["user-secrets", "set", key, value, "--project", WebServerProjectPath];
    return CaptureAsync("dotnet", arguments, skipOnDryRun: true, maskArguments: maskValue, maskAuthorizationHeader: false);
  }

  /// <summary>
  /// Always executes (including under --dry-run). Listing is read-only; the mint decision
  /// compares stored ClientId/TenantId against the target app.
  /// </summary>
  internal Task<CommandOutput> ListUserSecretsAsync()
  {
    string[] arguments = ["user-secrets", "list", "--project", WebServerProjectPath];
    return CaptureAsync(
      "dotnet",
      arguments,
      skipOnDryRun: false,
      maskArguments: false,
      maskAuthorizationHeader: false);
  }

  internal void WriteFailure(CommandOutput output, string message)
  {
    Terminal.WriteErrorLine(message.Red());
    if (!string.IsNullOrWhiteSpace(output.Stderr))
    {
      Terminal.WriteErrorLine(output.Stderr);
    }
  }

  private async Task<CommandOutput> CaptureAsync(
    string executable,
    IReadOnlyList<string> arguments,
    bool skipOnDryRun,
    bool maskArguments,
    bool maskAuthorizationHeader,
    bool printStdoutNever = false)
  {
    if (IsDryRun)
    {
      IReadOnlyList<string> printed = maskArguments
        ? MaskedArguments(arguments)
        : maskAuthorizationHeader
          ? MaskAuthorizationHeaders(arguments)
          : arguments;
      string suffix = printStdoutNever ? " (stdout not printed)" : "";
      Terminal.WriteLine($"dry-run: {EntraSetup.FormatInvocation(executable, printed)}{suffix}");
      if (skipOnDryRun)
      {
        return CommandOutput.Empty();
      }
    }

    // Never stream stdout: credential-reset password and access tokens live there.
    return await Shell.Builder(executable)
      .WithArguments([.. arguments])
      .WithWorkingDirectory(RepoRoot)
      .WithNoValidation()
      .CaptureAsync(Ct)
      .ConfigureAwait(false);
  }

  private static IReadOnlyList<string> MaskedArguments(IReadOnlyList<string> arguments)
  {
    // `dotnet user-secrets set <key> <value> --project <path>` — index 3 is the value.
    if (arguments.Count < 4)
    {
      return arguments;
    }

    string[] masked = [.. arguments];
    masked[3] = EntraSetup.MaskedSecret;
    return masked;
  }

  private static string[] MaskAuthorizationHeaders(IReadOnlyList<string> arguments)
  {
    string[] masked = [.. arguments];
    for (int index = 0; index < masked.Length; index++)
    {
      string argument = masked[index];
      const string bearerPrefix = "Authorization=Bearer ";
      if (argument.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
      {
        masked[index] = bearerPrefix + EntraSetup.MaskedSecret;
      }
    }

    return masked;
  }
}
