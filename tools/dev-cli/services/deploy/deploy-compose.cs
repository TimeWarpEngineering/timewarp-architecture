#region Purpose
// The Compose half of `dev open` / `dev deploy migrate`: the published `.env`, the running Compose
// projects, and the `compose exec` arguments.
#endregion

#region Design
// Compose project: `aspire deploy` chooses the Compose project name itself (the published
// docker-compose.yaml has no `name:`), so the verb never guesses it. It asks the runtime —
// `<runtime> compose ls --format json`, the running projects — and uses `--project-name` when the
// operator passes one; otherwise the one project whose compose file lives in this checkout, else the
// only running project; anything else is a refusal listing the candidates. The project's own compose
// file(s) from `compose ls` are passed with --file so exec resolves the postgres service.
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>The Compose half of <c>dev open</c> / <c>dev deploy migrate</c>: published .env, running projects and <c>compose exec</c>.</summary>
internal static class DeployCompose
{
  /// <summary>The value of <paramref name="name"/> in a <c>.env</c> file; null when absent or blank (the published .env leaves parameters blank).</summary>
  internal static string? ParseEnvValue(string envText, string name)
  {
    foreach (string raw in envText.Split('\n'))
    {
      string line = raw.Trim();
      if (line.StartsWith('#', StringComparison.Ordinal))
      {
        continue;
      }

      int separator = line.IndexOf('=', StringComparison.Ordinal);
      if (separator > 0 && line[..separator].Trim() == name)
      {
        string value = line[(separator + 1)..].Trim().Trim('"', '\'');
        return value.Length > 0 ? value : null;
      }
    }

    return null;
  }

  /// <summary>
  /// The Compose host port: <c>--port</c>, else <c>INGRESS_PORT</c> in the published <c>.env</c>, else the resolved
  /// <c>ingress-port</c> parameter. Returns the port and where it came from; (null, null) when none is set.
  /// </summary>
  internal static (string? Port, string? Source) ChooseComposePort(string? option, string? envValue, ResolvedDeployParameter? parameter) =>
    !string.IsNullOrWhiteSpace(option) ? (option.Trim(), "--port")
    : envValue is not null ? (envValue, $"{DeployOperate.IngressPortVariable} in the published .env")
    : parameter is not null ? (parameter.Value, $"{DeployOperate.IngressPortParameter} from the {parameter.Source}")
    : (null, null);

  /// <summary><c>&lt;runtime&gt; compose ls --format json</c>: the running Compose projects.</summary>
  internal static string[] BuildComposeListArguments() => ["compose", "ls", "--format", "json"];

  /// <summary>Parses <c>compose ls --format json</c> (Name, Status, ConfigFiles comma-separated); empty when unparseable.</summary>
  internal static ComposeProject[] ParseComposeProjects(string json)
  {
    try
    {
      using var document = JsonDocument.Parse(json);
      if (document.RootElement.ValueKind != JsonValueKind.Array)
      {
        return [];
      }

      return
      [
        .. document.RootElement.EnumerateArray()
          .Where(entry => entry.ValueKind == JsonValueKind.Object)
          .Select(entry => new ComposeProject(
            Text(entry, "Name"),
            Text(entry, "Status"),
            [.. Text(entry, "ConfigFiles").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]))
          .Where(project => project.Name.Length > 0),
      ];
    }
    catch (JsonException)
    {
      return [];
    }

    static string Text(JsonElement entry, string name) =>
      entry.EnumerateObject().FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)).Value is
      { ValueKind: JsonValueKind.String } value
        ? value.GetString() ?? ""
        : "";
  }

  /// <summary>
  /// The project to migrate: <paramref name="requested"/> when given (it must be running), else the one whose compose file
  /// is under <paramref name="repoRoot"/>, else the only running project; otherwise a refusal naming the candidates.
  /// </summary>
  internal static ComposeProjectChoice ChooseComposeProject(IReadOnlyList<ComposeProject> projects, string? requested, string repoRoot, string runtime)
  {
    if (!string.IsNullOrWhiteSpace(requested))
    {
      ComposeProject? named = projects.FirstOrDefault(project => string.Equals(project.Name, requested.Trim(), StringComparison.Ordinal));
      return named is not null
        ? new ComposeProjectChoice(named, null)
        : new ComposeProjectChoice(null, $"No running Compose project named '{requested.Trim()}'. {ComposeCandidates(projects, runtime)}");
    }

    string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repoRoot)) + Path.DirectorySeparatorChar;
    ComposeProject[] inCheckout =
    [
      .. projects.Where(project => project.ConfigFiles.Any(file => Path.GetFullPath(file).StartsWith(root, StringComparison.Ordinal))),
    ];
    if (inCheckout.Length == 1)
    {
      return new ComposeProjectChoice(inCheckout[0], null);
    }

    if (inCheckout.Length == 0 && projects.Count == 1)
    {
      return new ComposeProjectChoice(projects[0], null);
    }

    return projects.Count == 0
      ? new ComposeProjectChoice(null, $"No Compose project is running (`{runtime} compose ls` lists none). Deploy first: dev deploy --target compose")
      : new ComposeProjectChoice(null, $"Several Compose projects are running; name the deployment: dev deploy migrate --target compose --project-name <name>. {ComposeCandidates(projects, runtime)}");
  }

  private static string ComposeCandidates(IReadOnlyList<ComposeProject> projects, string runtime) =>
    projects.Count == 0
      ? $"`{runtime} compose ls` lists no running project."
      : $"Running (`{runtime} compose ls`): {string.Join(", ", projects.Select(project => project.Name))}.";

  /// <summary>
  /// <c>&lt;runtime&gt; compose … exec -T postgres</c> of <paramref name="psqlCommand"/> for <paramref name="project"/>, stdin
  /// attached for the script.
  /// </summary>
  internal static string[] BuildComposeMigrateArguments(ComposeProject project, string psqlCommand) =>
  [
    "compose", "--project-name", project.Name,
    .. project.ConfigFiles.SelectMany(file => (string[])["--file", file]),
    "exec", "-T", DeployOperate.PostgresResourceName, "sh", "-c", psqlCommand,
  ];
}
