#region Purpose
// Shared az / dotnet user-secrets process runner for `dev entra` with dry-run and secret-safe capture.
#endregion

#region Design
// Amuru Shell.Builder only — never System.Diagnostics.Process. Dry-run prints the invocation
// and returns a successful empty output without executing. Secret-bearing captures (credential
// reset stdout, user-secrets set of ClientSecret) never print stdout or the live argument list.
// az on PATH is PathResolver, not a thrown process start.
#endregion

namespace DevCli.Services;

internal sealed class EntraCli
{
  internal const string AzLoginHint = "az login is required. Run `az login` then retry.";

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
    CaptureAsync("az", arguments, maskArguments: false);

  internal Task<CommandOutput> CaptureAzSecretAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("az", arguments, maskArguments: false);

  internal Task<CommandOutput> CaptureDotNetAsync(IReadOnlyList<string> arguments) =>
    CaptureAsync("dotnet", arguments, maskArguments: false);

  internal Task<CommandOutput> SetUserSecretAsync(string key, string value, bool maskValue)
  {
    string[] arguments = ["user-secrets", "set", key, value, "--project", WebServerProjectPath];
    return CaptureAsync("dotnet", arguments, maskArguments: maskValue);
  }

  internal async Task<CommandOutput> ListUserSecretsAsync()
  {
    string[] arguments = ["user-secrets", "list", "--project", WebServerProjectPath];
    return await CaptureDotNetAsync(arguments).ConfigureAwait(false);
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
    bool maskArguments)
  {
    if (IsDryRun)
    {
      IReadOnlyList<string> printed = maskArguments ? MaskedArguments(arguments) : arguments;
      Terminal.WriteLine($"dry-run: {EntraSetup.FormatInvocation(executable, printed)}");
      return CommandOutput.Empty();
    }

    // Never stream stdout: credential-reset password lives there when CaptureAzSecretAsync is used.
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
}
