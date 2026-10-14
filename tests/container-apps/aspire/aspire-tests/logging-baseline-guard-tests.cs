#region Purpose
// Fail if committed configuration lowers a host's log levels into Debug territory or turns on EF
// Core SQL or sensitive-data logging by default.
#endregion

#region Design
// Task 358: web-server's base appsettings.json shipped Default=Debug, so every environment
// (Production included) logged EF Core command text per query. A long `dev run` wrote a 26 GB
// stdout log. These facts read the committed files, so the defaults cannot silently regress.
// Baseline: every server host (web-server, api-server, grpc-server, yarp) carries the SAME
// Logging:LogLevel section in its base appsettings.json: Default Information,
// Microsoft.Hosting.Lifetime Information (IngressSmoke waits for "Application started"), and
// Microsoft.AspNetCore, Microsoft.EntityFrameworkCore(.Database.Command), System.Net.Http.HttpClient,
// Microsoft.Extensions.Http, Polly, Yarp and Grpc at Warning. The baseline lives in each host's
// appsettings.json rather than in service defaults code: config is where an operator looks, and
// the equality fact below keeps the four copies in agreement. Hosts a template flag removed are
// skipped.
// Per environment: only appsettings.Development.json may go below Information, and only for named
// categories (the app's own TimeWarp.Architecture namespaces at Debug). Default never drops below
// Information and the EF categories never below Warning in any committed file. EF SQL is a
// per-machine opt-in: user secrets or the environment set
// Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Information.
// EnableSensitiveDataLogging may only appear in a file that gates it on IsDevelopment(). The
// publish suites separately assert no published service sets Logging__LogLevel__* to Debug/Trace.
#endregion

namespace LoggingBaselineGuard_;

using System.Text.Json;

[TestTag("Unit")]
public partial class LoggingBaseline_Should_
{
  private static readonly string[] Levels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"];

  private static readonly string[] ServerHosts =
  [
    "source/container-apps/web/projects/web-server",
    "source/container-apps/api/projects/api-server",
    "source/container-apps/grpc/projects/grpc-server",
    "source/container-apps/yarp",
  ];

  private static readonly string[] WarningCategories =
  [
    "Microsoft.EntityFrameworkCore",
    "Microsoft.EntityFrameworkCore.Database.Command",
  ];

  private static readonly JsonDocumentOptions JsonOptions = new()
  {
    CommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true,
  };

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<LoggingBaseline_Should_>();

  public static Task Be_Identical_Across_Server_Hosts()
  {
    string root = FindRepositoryRoot();
    List<(string Host, Dictionary<string, string> LogLevel)> sections = [];
    foreach (string host in ServerHosts)
    {
      string file = Path.Combine(root, host, "appsettings.json");
      if (File.Exists(file))
      {
        sections.Add((host, ReadLogLevel(file)));
      }
    }

    sections.ShouldNotBeEmpty("no server host appsettings.json found");
    (string firstHost, Dictionary<string, string> first) = sections[0];
    foreach ((string host, Dictionary<string, string> logLevel) in sections.Skip(1))
    {
      logLevel.OrderBy(entry => entry.Key, StringComparer.Ordinal).ShouldBe(
        first.OrderBy(entry => entry.Key, StringComparer.Ordinal),
        $"{host} Logging:LogLevel differs from {firstHost}");
    }

    return Task.CompletedTask;
  }

  public static Task Keep_Base_Config_At_Information_With_Ef_At_Warning()
  {
    string root = FindRepositoryRoot();
    foreach (string host in ServerHosts)
    {
      string file = Path.Combine(root, host, "appsettings.json");
      if (!File.Exists(file))
      {
        continue;
      }

      Dictionary<string, string> logLevel = ReadLogLevel(file);
      logLevel.ShouldContainKey("Default", file);
      Rank(logLevel["Default"]).ShouldBeGreaterThanOrEqualTo(Rank("Information"), $"{file} Default");
      foreach (string category in WarningCategories)
      {
        logLevel.ShouldContainKey(category, file);
        Rank(logLevel[category]).ShouldBeGreaterThanOrEqualTo(Rank("Warning"), $"{file} {category}");
      }
    }

    return Task.CompletedTask;
  }

  public static Task Allow_Debug_Only_For_Named_Categories_In_Development()
  {
    string root = FindRepositoryRoot();
    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "source"), "appsettings*.json", SearchOption.AllDirectories))
    {
      if (IsBuildOutput(Path.GetRelativePath(root, file)))
      {
        continue;
      }

      bool development = Path.GetFileName(file).Equals("appsettings.Development.json", StringComparison.OrdinalIgnoreCase);
      foreach ((string category, string level) in ReadLogLevel(file))
      {
        int floor =
          WarningCategories.Contains(category) ? Rank("Warning")
          : development && category != "Default" ? Rank("Trace")
          : Rank("Information");
        if (Rank(level) < floor)
        {
          offenders.Add($"{Path.GetRelativePath(root, file)}: {category}={level}");
        }
      }
    }

    offenders.ShouldBeEmpty(string.Join(Environment.NewLine, offenders));
    return Task.CompletedTask;
  }

  public static Task Gate_Sensitive_Data_Logging_On_Development()
  {
    string root = FindRepositoryRoot();
    List<string> offenders = [];
    foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "source"), "*.cs", SearchOption.AllDirectories))
    {
      if (IsBuildOutput(Path.GetRelativePath(root, file)))
      {
        continue;
      }

      string text = File.ReadAllText(file);
      if (text.Contains("EnableSensitiveDataLogging(", StringComparison.Ordinal)
        && !text.Contains("IsDevelopment()", StringComparison.Ordinal))
      {
        offenders.Add(Path.GetRelativePath(root, file));
      }
    }

    offenders.ShouldBeEmpty("EnableSensitiveDataLogging must be gated on IsDevelopment(). Offenders: " + string.Join(", ", offenders));
    return Task.CompletedTask;
  }

  private static Dictionary<string, string> ReadLogLevel(string file)
  {
    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file), JsonOptions);
    Dictionary<string, string> result = new(StringComparer.Ordinal);
    if (document.RootElement.TryGetProperty("Logging", out JsonElement logging)
      && logging.TryGetProperty("LogLevel", out JsonElement logLevel))
    {
      foreach (JsonProperty property in logLevel.EnumerateObject())
      {
        result[property.Name] = property.Value.GetString() ?? string.Empty;
      }
    }

    return result;
  }

  private static int Rank(string level)
  {
    int rank = Array.FindIndex(Levels, candidate => candidate.Equals(level, StringComparison.OrdinalIgnoreCase));
    rank.ShouldBeGreaterThanOrEqualTo(0, $"unknown log level '{level}'");
    return rank;
  }

  private static bool IsBuildOutput(string relativePath)
  {
    string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return segments.Contains("bin") || segments.Contains("obj");
  }

  private static string FindRepositoryRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
  }
}
