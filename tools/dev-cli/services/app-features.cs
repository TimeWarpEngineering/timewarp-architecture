#region Purpose
// Which template features (web, yarp, postgres) this checkout has, so a command that needs one
// refuses cleanly in an app generated without it.
#endregion

#region Design
// Task 530. The template pack ships the dev CLI to every generated app, flag-off ones included,
// where `dotnet new` removed a feature's projects. template.json excludes `dev db` from an app
// without postgres or web. The commands below stay, because shared deploy code compiles them in,
// and each checks its feature before any preflight: entra (web: the web-server user secrets),
// open and deploy (yarp: the ingress resource they address), deploy migrate (postgres: the EF
// migrations). `dev run` skips its web-only xAI key warning instead of refusing.
// A feature is present when its marker path exists under the repository root. Each marker is a
// path template.json removes with that flag off. This repository has every feature.
// A missing repository root passes: the command then reports it its own way.
// The usings are file-level so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

using TimeWarp.Amuru;
using TimeWarp.Terminal;

/// <summary>A template feature flag and the repo-relative path that exists only when it is on.</summary>
internal sealed record AppFeature(string Flag, string MarkerPath);

/// <summary>Feature checks for commands that need a template feature this app may not have.</summary>
internal static class AppFeatures
{
  internal static readonly AppFeature Web =
    new("web", "source/container-apps/web/projects/web-server/web-server.csproj");

  internal static readonly AppFeature Yarp =
    new("yarp", "source/container-apps/yarp/yarp.csproj");

  internal static readonly AppFeature Postgres =
    new("postgres", "source/container-apps/web/platform/postgres/migrations");

  internal static bool IsPresent(string repoRoot, AppFeature feature)
  {
    ArgumentNullException.ThrowIfNull(feature);
    string path = Path.Combine(repoRoot, feature.MarkerPath);
    return File.Exists(path) || Directory.Exists(path);
  }

  internal static string MissingMessage(string verb, AppFeature feature)
  {
    ArgumentNullException.ThrowIfNull(feature);
    return $"{verb} needs the {feature.Flag} feature. This app has no {feature.MarkerPath}: it was generated with --{feature.Flag} false.";
  }

  /// <summary>True when the feature is present; otherwise prints why <paramref name="verb"/> refuses and sets exit code 1.</summary>
  internal static bool Require(ITerminal terminal, string verb, AppFeature feature)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    string? root = Git.FindRoot();
    if (root is null || IsPresent(root, feature)) return true;

    terminal.WriteErrorLine(MissingMessage(verb, feature).Red());
    Environment.ExitCode = 1;
    return false;
  }
}
