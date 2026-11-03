#region Purpose
// Shared resource names, published-output paths and summary text for `dev open` and
// `dev deploy migrate` (reach and migrate a deployment `dev deploy` made), per deploy target.
#endregion

#region Design
// Pure helpers (no Amuru/Terminal) so tests/tools/dev-cli-tests Compile-include them and gate both
// verbs without a cluster, a container runtime or Azure (task 287). The process half is the two
// command handlers plus services/aspire-deploy-preflight.cs (shared with `dev deploy`).
//
// The former single DeployOperate class is split by responsibility, all in services/deploy/:
// DeployOperate (names, published output, summary), DeployKubernetes, DeployCompose, DeployAca,
// DeployMigrationHistory, DeployStaleScript and DeployBrowserChoice.
//
// Wrap the tool, never its internals (070-006 / 284): every value comes from the deploy configuration
// (Parameters:* resolved exactly as `dev deploy` resolves them — AspireDeployParameters.ResolveParameters), the
// published output under artifacts/aspire-output/<target>, or a documented CLI (kubectl, az, the
// container runtime's `compose ls`). The resource names below are the AppHost's (constants.cs:
// YarpResourceName, PostgresResourceName, PostgresDatabaseResourceName) and Aspire's published names
// derived from them (statefulset/postgres-statefulset); dev-cli-tests reads constants.cs so the copies
// cannot drift.
//
// Missing published output (task 393): `dev deploy migrate` publishes that target (`aspire publish`
// into artifacts/aspire-output/<target>) and continues, instead of refusing. The line still names
// `dev publish <target>`. The production-safety suite stays on `dev publish`; this publish only
// writes the files the migration runs.
#endregion

namespace DevCli.Services;

/// <summary>Shared names, published-output paths and summary text for <c>dev open</c> / <c>dev deploy migrate</c>.</summary>
internal static class DeployOperate
{
  // AppHost constants.cs: YarpResourceName, PostgresResourceName, PostgresDatabaseResourceName (dev-cli-tests agree).
  internal const string IngressResourceName = "ingress";
  internal const string PostgresResourceName = "postgres";
  internal const string PostgresDatabaseName = "postgres-db";
  internal const string PostgresStatefulSet = "statefulset/postgres-statefulset";
  internal const string IngressPortParameter = "ingress-port";
  internal const string IngressPortVariable = "INGRESS_PORT";

  internal const string MigrationScript = "efmigrations/web-migrations.sql";
  internal const string MigrationBundle = "efmigrations/web-migrations";

  /// <summary>Where <c>dev publish &lt;target&gt;</c> writes the target's output, relative to the repository root.</summary>
  internal static string PublishedOutputDirectory(DeployTarget target) => $"artifacts/aspire-output/{target.Name}";

  /// <summary>The published file <c>dev deploy migrate</c> runs for <paramref name="target"/>: the bundle for aca, else the SQL script.</summary>
  internal static string MigrationArtifact(DeployTarget target) =>
    AspireDeploy.UsesMigrationBundle(target) ? MigrationBundle : MigrationScript;

  /// <summary>Printed when the migration artifact is missing: this verb publishes it, and names <c>dev publish</c>.</summary>
  internal static string MissingPublishedOutputMessage(DeployTarget target, string path) =>
    DeployPipeline.WillPublishMissingMigrations(target, path);

  /// <summary>The migrate command as an operator would type it in pwsh: the script piped on stdin (<c>Get-Content -Raw</c>).</summary>
  internal static string BuildPipedCommandDisplay(string script, string executable, IReadOnlyList<string> arguments) =>
    $"Get-Content -Raw {PwshQuote(script)} | {executable} {string.Join(' ', arguments.Select(PwshQuote))}";

  /// <summary>pwsh single-quoted literal (embedded ' doubled) when the argument needs quoting; as-is otherwise.</summary>
  internal static string PwshQuote(string argument) =>
    argument.Length > 0 && argument.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '/' or ':' or '=')
      ? argument
      : $"'{argument.Replace("'", "''", StringComparison.Ordinal)}'";

  internal const string MigrateConfirmationRefusal =
    "Not migrating: no confirmation. Re-run with --yes to migrate non-interactively, or from a terminal to answer the prompt.";

  internal const string MigrateDeclined = "Not migrating: declined at the prompt. Nothing was run.";

  /// <summary>The one summary line: what ran against which target, and how it ended.</summary>
  internal static string BuildMigrateSummary(DeployTarget target, string ran, string against, int exitCode) =>
    exitCode == 0
      ? $"dev deploy migrate: applied {ran} to {target.Name} ({against})."
      : $"dev deploy migrate: {ran} against {target.Name} ({against}) failed (exit {exitCode}).";
}
