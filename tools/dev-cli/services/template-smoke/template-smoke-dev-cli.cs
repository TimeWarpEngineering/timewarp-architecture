#region Purpose
// Template smoke: the generated app ships the dev CLI without its template-only parts, and the CLI
// builds, lists the app's commands, and runs `dev build` there.
#endregion
#region Design
// Task 530. AssertDevCliShipped runs right after generation (no restore): the CLI files and the
// root files it reads exist; every tools/ path that template.json excludes unconditionally is
// absent (the list is read from .template.config/template.json, never copied here); `dev db` is
// present exactly when postgres and web are both on; and no templateRepository directive survived
// generation under tools/ or .timewarp/.
// RunGeneratedCliAsync runs after the generated solution builds, through the runfile path a fresh
// clone uses (`dotnet run tools/dev-cli/dev.cs`), so it needs no AOT publish. It git-inits the app
// first: the CLI finds its root with Git.FindRoot, and without a .git the walk would reach this
// repository's root above artifacts/template-smoke. `--capabilities` (Nuru's JSON endpoint list; the
// --help table truncates names; a building run prints its warnings first) must list every app command and none of the template-only ones
// (TemplateOnlyCommands, which includes check-version and release, the TimeWarp.Nuru.DevCli
// endpoints the CLI's Directory.Build.props removes). A command is the first word of an endpoint
// pattern, so `db update` counts as db. Then `dev build` must exit 0.
#endregion

namespace DevCli.Services;

using System.Text.Json;

/// <summary>
/// Shipped-file and run checks for the dev CLI in a generated app.
/// </summary>
internal sealed class TemplateSmokeDevCli
{
  internal const string DevCliRunfile = "tools/dev-cli/dev.cs";

  private static readonly string[] ShippedFiles =
  [
    "tools/Directory.Build.props",
    "tools/dev-cli/Directory.Build.props",
    DevCliRunfile,
    "tools/dev-cli/global-usings.cs",
    ".timewarp/dev.jsonc",
    ".envrc",
  ];

  // Present only when postgres and web are both on (template.json condition (!postgres || !web)).
  private const string DbGroupFile = "tools/dev-cli/endpoints/db-group.cs";

  private static readonly string[] AppCommands =
    ["build", "test", "run", "publish", "deploy", "open", "deprovision", "entra", "workflow", "clean", "self-install"];

  private static readonly string[] TemplateOnlyCommands =
  [
    "template-smoke",
    "template-install",
    "template-publish-smoke",
    "check-client-names",
    "lint-skills",
    "verify-samples",
    "regenerate-generated",
    "check-version",
    "release",
  ];

  // dev db and its top-level db-update alias.
  private static readonly string[] DbCommands = ["db", "db-update"];

  private static readonly string[] ScannedRoots = ["tools", ".timewarp"];

  private static readonly string[] TemplateDirectiveForms =
    ["#if templateRepository", "#if (templateRepository)", "#if (!templateRepository)"];

  private readonly ITerminal Terminal;
  private readonly string RepoRoot;

  public TemplateSmokeDevCli(ITerminal terminal, string repoRoot)
  {
    Terminal = terminal;
    RepoRoot = repoRoot;
  }

  /// <summary>File-level checks on the generated app's dev CLI. <paramref name="dbExpected"/> is postgres and web on.</summary>
  public bool AssertDevCliShipped(string outputDir, bool dbExpected)
  {
    bool ok = true;
    foreach (string relative in ShippedFiles)
    {
      if (!File.Exists(Path.Combine(outputDir, relative)))
      {
        Terminal.WriteErrorLine($"Generated app is missing {relative}: the template pack must ship the dev CLI (task 530).".Red());
        ok = false;
      }
    }

    string[]? excluded = ReadUnconditionalToolExcludes();
    if (excluded is null) return false;

    foreach (string pattern in excluded)
    {
      string relative = pattern.EndsWith("/**", StringComparison.Ordinal) ? pattern[..^3] : pattern;
      string path = Path.Combine(outputDir, relative);
      if (File.Exists(path) || Directory.Exists(path))
      {
        Terminal.WriteErrorLine($"Generated app contains {relative}, which template.json excludes as template-only.".Red());
        ok = false;
      }
    }

    bool dbShipped = File.Exists(Path.Combine(outputDir, DbGroupFile));
    if (dbShipped != dbExpected)
    {
      Terminal.WriteErrorLine(
        $"Generated app {(dbShipped ? "contains" : "is missing")} {DbGroupFile}; `dev db` ships exactly when postgres and web are on.".Red());
      ok = false;
    }

    foreach (string root in ScannedRoots.Select(root => Path.Combine(outputDir, root)).Where(Directory.Exists))
    {
      foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
      {
        string text = File.ReadAllText(file);
        if (TemplateDirectiveForms.Any(form => text.Contains(form, StringComparison.Ordinal)))
        {
          Terminal.WriteErrorLine($"Generated {Path.GetRelativePath(outputDir, file)} still holds a templateRepository directive.".Red());
          ok = false;
        }
      }
    }

    if (ok) Terminal.WriteLine("Generated app ships the dev CLI without its template-only files.");
    return ok;
  }

  /// <summary>
  /// Runs the generated CLI from its runfile: `--capabilities` lists the app commands and no
  /// template-only command, then `dev build` exits 0.
  /// </summary>
  public async Task<bool> RunGeneratedCliAsync(string outputDir, bool dbExpected, CancellationToken ct)
  {
    if (!Directory.Exists(Path.Combine(outputDir, ".git")))
    {
      CommandOutput init = await Shell.Builder("git")
        .WithArguments("init", "--quiet")
        .WithWorkingDirectory(outputDir)
        .WithNoValidation()
        .CaptureAsync(ct);
      if (!init.Success)
      {
        Terminal.WriteErrorLine(init.Combined);
        Terminal.WriteErrorLine("git init failed in the generated app.".Red());
        return false;
      }
    }

    Terminal.WriteLine($"Running the generated dev CLI: dotnet run {DevCliRunfile} -- --capabilities...");
    CommandOutput capabilities = await DevCli(outputDir, "--capabilities").CaptureAsync(ct);
    HashSet<string>? listed = capabilities.Success ? ListedCommands(capabilities.Stdout) : null;
    if (listed is null)
    {
      Terminal.WriteErrorLine(capabilities.Combined);
      Terminal.WriteErrorLine($"Generated dev CLI did not build, run, or print its capabilities (exit {capabilities.ExitCode}).".Red());
      return false;
    }

    string[] expected = dbExpected ? [.. AppCommands, "db"] : AppCommands;
    string[] missing = [.. expected.Where(command => !listed.Contains(command))];
    string[] unexpected = [.. TemplateOnlyCommands.Where(listed.Contains)];
    if (!dbExpected) unexpected = [.. unexpected, .. DbCommands.Where(listed.Contains)];

    if (missing.Length > 0 || unexpected.Length > 0)
    {
      Terminal.WriteErrorLine($"Listed: {string.Join(", ", listed.Order(StringComparer.Ordinal))}");
      if (missing.Length > 0)
        Terminal.WriteErrorLine($"Generated dev CLI is missing: {string.Join(", ", missing)}.".Red());
      if (unexpected.Length > 0)
        Terminal.WriteErrorLine($"Generated dev CLI lists template-only or flag-off commands: {string.Join(", ", unexpected)}.".Red());
      return false;
    }

    Terminal.WriteLine("Running the generated dev CLI: dev build...");
    CommandOutput build = await DevCli(outputDir, "build").CaptureAsync(ct);
    if (!build.Success)
    {
      Terminal.WriteErrorLine(build.Combined);
      Terminal.WriteErrorLine($"Generated `dev build` failed (exit {build.ExitCode}).".Red());
      return false;
    }

    Terminal.WriteLine("Generated dev CLI lists the app commands and `dev build` passed.".Green());
    return true;
  }

  // The first word of each endpoint pattern in the `--capabilities` JSON; null when it does not parse.
  // A first `dotnet run` prints its build warnings to stdout ahead of the JSON, which starts on a line
  // of its own.
  internal static HashSet<string>? ListedCommands(string stdout)
  {
    int start = stdout.StartsWith('{') ? 0 : stdout.IndexOf("\n{", StringComparison.Ordinal) + 1;
    if (start < 0 || start >= stdout.Length || stdout[start] != '{') return null;

    try
    {
      using var document = JsonDocument.Parse(stdout[start..]);
      return
      [
        .. document.RootElement.GetProperty("endpoints").EnumerateArray()
          .Select(endpoint => (endpoint.GetProperty("pattern").GetString() ?? "").Trim())
          .Select(pattern => pattern.Split(' ', 2)[0])
          .Where(command => command.Length > 0),
      ];
    }
    catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
    {
      return null;
    }
  }

  private static DotNetRunBuilder DevCli(string outputDir, params string[] arguments) =>
    DotNet.Run()
      .WithFile(Path.Combine(outputDir, DevCliRunfile))
      .WithArguments(arguments)
      .WithWorkingDirectory(outputDir)
      .WithNoValidation();

  // tools/ entries of the template.json modifier whose condition is (true).
  private string[]? ReadUnconditionalToolExcludes()
  {
    string path = Path.Combine(RepoRoot, ".template.config", "template.json");
    try
    {
      using var document = JsonDocument.Parse(File.ReadAllText(path));
      return
      [
        .. document.RootElement.GetProperty("sources").EnumerateArray()
          .SelectMany(source => source.GetProperty("modifiers").EnumerateArray())
          .Where(modifier => modifier.GetProperty("condition").GetString() == "(true)")
          .SelectMany(modifier => modifier.GetProperty("exclude").EnumerateArray())
          .Select(entry => entry.GetString() ?? "")
          .Where(entry => entry.StartsWith("tools/", StringComparison.Ordinal)),
      ];
    }
    catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
    {
      Terminal.WriteErrorLine($"Could not read the tools/ excludes from {path}: {exception.Message}".Red());
      return null;
    }
  }
}
