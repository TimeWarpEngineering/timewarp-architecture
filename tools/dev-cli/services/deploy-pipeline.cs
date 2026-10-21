#region Purpose
// Step order, skip text, and stop-on-failure text for the one-command `dev deploy`.
#endregion

#region Design
// Pure so dev-cli-tests can lock the order without Aspire, a cluster, or Azure (task 393).
// The order is publish, deploy, migrate, smoke. Migrate is omitted when the app has no
// database (no platform/postgres/migrations folder). There is no per-step opt-out flag:
// `dev publish`, `dev deploy migrate`, and `dev open` remain the one-step verbs.
// The handler runs that list through RunAsync. Each step delegate returns an exit code.
// The first non-zero code stops the run, and later steps are not called.
// A failure names the verb that retries that step and does not imply a later step ran.
// Smoke is the two anonymous paths the Neon guide documents. It retries transport failures
// and HTTP 5xx inside a budget so a Neon wake-up or a scale-to-zero start can finish.
// It never requests /api/health. Secrets are not part of these strings.
// After smoke succeeds, FollowUpAfterSmoke says whether to open the browser.
// A temporary kubernetes port-forward does not call it. That path prints CompleteMessage,
// then TemporaryForwardStopped, which names `dev open`, and the forward is not left running.
// --no-browser prints the URL. --yes is not an input. The launcher is DeployBrowser.
#endregion

namespace DevCli.Services;

/// <summary>What a successful smoke prints, and whether <c>dev deploy</c> opens a browser.</summary>
internal sealed record SmokeFollowUp(bool OpenBrowser, string Summary);

/// <summary>Plans the one-command <c>dev deploy</c> and the message printed when a step stops it.</summary>
internal static class DeployPipeline
{
  internal const string Publish = "publish";
  internal const string Deploy = "deploy";
  internal const string Migrate = "migrate";
  internal const string Smoke = "smoke";

  /// <summary>Anonymous ingress checks. Not <c>/api/health</c> (that opens Postgres and would keep Neon awake).</summary>
  internal static readonly string[] SmokePaths = ["/web-server/alive", "/api/identity/entra/offered"];

  /// <summary>How long smoke may retry a cold ingress or a waking Neon database.</summary>
  internal static readonly TimeSpan SmokeBudget = TimeSpan.FromMinutes(4);

  /// <summary>Pause between smoke attempts after a retryable failure. Not a health-poll timer.</summary>
  internal static readonly TimeSpan SmokeDelay = TimeSpan.FromSeconds(5);

  /// <summary>One smoke HTTP call. A Neon cold start can spend most of this inside the request.</summary>
  internal static readonly TimeSpan SmokeAttemptTimeout = TimeSpan.FromSeconds(90);

  /// <summary>Publish, deploy, then migrate when <paramref name="hasDatabase"/>, then smoke.</summary>
  internal static string[] Steps(bool hasDatabase) =>
    hasDatabase ? [Publish, Deploy, Migrate, Smoke] : [Publish, Deploy, Smoke];

  /// <summary>
  /// Runs <see cref="Steps"/> in order. <paramref name="runStep"/> returns that step's exit code.
  /// The first non-zero code is returned and later steps are not called.
  /// </summary>
  internal static async Task<int> RunAsync(
    bool hasDatabase,
    Func<string, CancellationToken, Task<int>> runStep,
    CancellationToken cancellationToken)
  {
    foreach (string step in Steps(hasDatabase))
    {
      cancellationToken.ThrowIfCancellationRequested();
      int exitCode = await runStep(step, cancellationToken);
      if (exitCode != 0)
      {
        return exitCode;
      }
    }

    return 0;
  }

  /// <summary>True when a smoke response should be tried again: no response, or an HTTP 5xx.</summary>
  internal static bool ShouldRetrySmoke(int? statusCode) => statusCode is null or >= 500;

  /// <summary><paramref name="baseUrl"/> plus one smoke path, with a single slash between them.</summary>
  internal static string SmokeUrl(string baseUrl, string path) => baseUrl.TrimEnd('/') + path;

  /// <summary>Printed when migrate is left out because the app has no Postgres migrations folder.</summary>
  internal static string SkipMigrateMessage(DeployTarget target) =>
    $"dev deploy: {target.Name} has no database (no platform/postgres/migrations beside the AppHost), so migrate is skipped.";

  /// <summary>Printed when <c>dev deploy migrate</c> finds no bundle or script and publishes it.</summary>
  internal static string WillPublishMissingMigrations(DeployTarget target, string path) =>
    $"No published migrations for {target.Name}: {path} does not exist. Publishing them now, then continuing: dev publish {target.Name}";

  /// <summary>The line that stops the pipeline. The re-run verb is the one for <paramref name="step"/> only.</summary>
  internal static string FailureMessage(string step, DeployTarget target, int exitCode) => step switch
  {
    Publish => $"dev deploy: publish failed (exit {exitCode}). Nothing else ran. Re-run: dev publish {target.Name}",
    Deploy => $"dev deploy: aspire deploy failed (exit {exitCode}). Publish already finished. Re-run: dev deploy --target {target.Name}",
    Migrate => $"dev deploy: migrate failed (exit {exitCode}). Publish and deploy already finished. Re-run: dev deploy migrate --target {target.Name}",
    Smoke => $"dev deploy: smoke failed (exit {exitCode}). The deployment is up. Re-check {string.Join(" and ", SmokePaths)}, or re-run: dev deploy --target {target.Name}",
    _ => $"dev deploy: {step} failed (exit {exitCode}). Re-run: dev deploy --target {target.Name}",
  };

  /// <summary>Heading printed before a step runs.</summary>
  internal static string StepBanner(string step, DeployTarget target) => $"dev deploy: {step} ({target.Name})";

  /// <summary>Confirmation prompt. Names the steps that will run.</summary>
  internal static string ConfirmPrompt(bool hasDatabase, bool noBrowser)
  {
    string finish = noBrowser ? "print the URL" : "open the app";
    return hasDatabase
      ? $"Deploy (publish, deploy, migrate, smoke, {finish})? [y/N] "
      : $"Deploy (publish, deploy, smoke, {finish}; migrate skipped, no database)? [y/N] ";
  }

  /// <summary>Printed after smoke: the URL and how to remove the deployment.</summary>
  internal static string CompleteMessage(DeployTarget target, string url) =>
    $"{target.Name} is up at {url}. Remove it with `dev deprovision --target {target.Name}`.";

  /// <summary>
  /// After smoke succeeds. Opens the ingress unless <paramref name="noBrowser"/>.
  /// A temporary kubernetes port-forward does not call this method. That path prints
  /// <see cref="TemporaryForwardStopped"/> after the forward stops. <c>--yes</c> is not an input.
  /// </summary>
  internal static SmokeFollowUp FollowUpAfterSmoke(DeployTarget target, string url, bool noBrowser) =>
    new(!noBrowser, CompleteMessage(target, url));

  /// <summary>Printed after a kubernetes smoke that used a port-forward. The forward is not left running.</summary>
  internal static string TemporaryForwardStopped(DeployTarget target) =>
    $"dev deploy: the temporary port-forward for {target.Name} has stopped. `dev open --target {target.Name}` keeps one.";

  /// <summary>Smoke could not read the ACA ingress FQDN because no resource group is known.</summary>
  internal static string SmokeNeedsResourceGroup(DeployTarget target) =>
    $"dev deploy: smoke needs the resource group to read the {target.Name} ingress host. "
    + "Set Azure__ResourceGroup or the AppHost user secret Azure:ResourceGroup (the deployment is up). "
    + $"Re-run: dev deploy --target {target.Name}";

  /// <summary>
  /// A problem with <c>Parameters:passkey-rp-host</c>, or null when it is empty or a bare DNS name.
  /// The value is a hostname, not a secret; the message does not need to echo it.
  /// </summary>
  internal static string? PasskeyHostProblem(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    string host = value.Trim();
    return Uri.CheckHostName(host) == UriHostNameType.Dns
      ? null
      : "Parameters:passkey-rp-host must be a bare DNS host name (no scheme, port, path, or IP). "
        + "It is appended to WebAuthnOptions:AllowedRpIds. localhost is already allowed.";
  }
}
