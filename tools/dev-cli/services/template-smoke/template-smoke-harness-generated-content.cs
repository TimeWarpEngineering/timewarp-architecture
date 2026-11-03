#region Purpose
// TemplateSmokeHarness partial: asserts on shipped content of a generated app (skills, kanban, deploy parameters, ingress port).
#endregion
#region Design
// AssertSkillsShipped: generated apps must contain the nine skills/*/SKILL.md files and must not
// contain any analysis/ directory under skills/ (pack exclude).
// Task 318: AssertKanbanSkeletonShipped - generated apps get the empty board skeleton (five column
// .gitkeep files, overview.md, task-template.md) and nothing else under kanban/.
// Task 288: AssertDeployParametersUseAppKebabName is the loud failure for an invalid kebab name.
// GenerateAsync (generation partial) is the generate-only half used for names that only need that
// cheap assert (template-smoke's Contoso.Shop).
// Task 311: ReadGeneratedIngressPort - the ingressPort port-generator symbol rewrites the committed
// Parameters:ingress-port. The range and the monorepo value it replaces are read from
// .template.config/template.json (symbols.ingressPort), never copied here, so the assert follows
// the template. The generator is random per generation; template-smoke collects every generation's
// port and fails when they are all the same (the value was not regenerated).
#endregion

namespace DevCli.Services;

using System.Text.Json;
using static DevCli.Services.TemplateSmokeAppHostParameters;

internal sealed partial class TemplateSmokeHarness
{
  /// <summary>
  /// Generated apps must receive repo skills (packaged from skills/**) and must not receive
  /// any analysis/ directory under skills/ (excluded from the template pack).
  /// </summary>
  public bool AssertSkillsShipped(string outputDir)
  {
    string skillsDir = Path.Combine(outputDir, "skills");
    if (!Directory.Exists(skillsDir))
    {
      Terminal.WriteErrorLine("Generated app is missing skills/ — template pack did not include skills/**.".Red());
      return false;
    }

    string[] requiredSkillFiles =
    [
      "tw-aggregate-pattern/SKILL.md",
      "tw-blazor-css-strategy/SKILL.md",
      "tw-blazor-layout/SKILL.md",
      "tw-blazor/SKILL.md",
      "tw-deploy/SKILL.md",
      "tw-feature-placement/SKILL.md",
      "tw-mock-response-factory/SKILL.md",
      "tw-slice-isolation/SKILL.md",
      "tw-web-api-contracts/SKILL.md",
    ];

    bool ok = true;
    foreach (string relative in requiredSkillFiles)
    {
      string path = Path.Combine(skillsDir, relative.Replace('/', Path.DirectorySeparatorChar));
      if (!File.Exists(path))
      {
        Terminal.WriteErrorLine($"Generated app is missing skills/{relative}.".Red());
        ok = false;
      }
    }

    foreach (string analysisDir in Directory.EnumerateDirectories(skillsDir, "analysis", SearchOption.AllDirectories))
    {
      string relativeAnalysis = Path.GetRelativePath(skillsDir, analysisDir).Replace('\\', '/');
      Terminal.WriteErrorLine($"Generated app contains skills/{relativeAnalysis} — analysis/ must not ship.".Red());
      ok = false;
    }

    if (ok)
      Terminal.WriteLine("Generated app contains skills/ (nine SKILL.md files; analysis/ excluded).");

    return ok;
  }

  /// <summary>
  /// Task 318: generated apps get an empty board — kanban/{backlog,to-do,in-progress,done,archived}/
  /// each with a .gitkeep, plus kanban/overview.md and kanban/task-template.md — and no task
  /// folders, task files or journals from this repo's board.
  /// </summary>
  public bool AssertKanbanSkeletonShipped(string outputDir)
  {
    string kanbanDir = Path.Combine(outputDir, "kanban");
    if (!Directory.Exists(kanbanDir))
    {
      Terminal.WriteErrorLine("Generated app is missing kanban/ — template pack did not include the board skeleton.".Red());
      return false;
    }

    string[] columns = ["backlog", "to-do", "in-progress", "done", "archived"];
    string[] expectedFiles =
    [
      .. columns.Select(column => $"{column}/.gitkeep"),
      "overview.md",
      "task-template.md",
    ];

    bool ok = true;
    foreach (string relative in expectedFiles)
    {
      if (!File.Exists(Path.Combine(kanbanDir, relative.Replace('/', Path.DirectorySeparatorChar))))
      {
        Terminal.WriteErrorLine($"Generated app is missing kanban/{relative}.".Red());
        ok = false;
      }
    }

    HashSet<string> expected = [.. expectedFiles];
    foreach (string file in Directory.EnumerateFiles(kanbanDir, "*", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(kanbanDir, file).Replace('\\', '/');
      if (!expected.Contains(relative))
      {
        Terminal.WriteErrorLine($"Generated app contains kanban/{relative} — only the empty board skeleton may ship.".Red());
        ok = false;
      }
    }

    HashSet<string> expectedDirectories = [.. columns];
    foreach (string directory in Directory.EnumerateDirectories(kanbanDir, "*", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(kanbanDir, directory).Replace('\\', '/');
      if (!expectedDirectories.Contains(relative))
      {
        Terminal.WriteErrorLine($"Generated app contains kanban/{relative}/ — task folders must not ship.".Red());
        ok = false;
      }
    }

    if (ok)
      Terminal.WriteLine("Generated app contains the empty kanban/ skeleton (five columns, overview, task template).");

    return ok;
  }

  /// <summary>
  /// Task 288: the generated AppHost appsettings.json commits k8s-namespace, helm-release-name and registry-repository as
  /// <paramref name="expectedKebabName"/> (the appNameKebab derived symbol), each a DNS-1123 label — never the monorepo's own
  /// timewarp-architecture.
  /// </summary>
  public bool AssertDeployParametersUseAppKebabName(string outputDir, string expectedKebabName)
  {
    string path = Path.Combine(outputDir, AppHostAppSettingsRelativePath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(path))
    {
      Terminal.WriteErrorLine($"Generated app is missing {AppHostAppSettingsRelativePath}.".Red());
      return false;
    }

    JsonElement? parameters = null;
    try
    {
      using var document = JsonDocument.Parse(File.ReadAllText(path));
      if (document.RootElement.TryGetProperty("Parameters", out JsonElement section) && section.ValueKind == JsonValueKind.Object)
        parameters = section.Clone();
    }
    catch (JsonException exception)
    {
      Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} is not valid JSON: {exception.Message}".Red());
      return false;
    }

    if (parameters is not { } values)
    {
      Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} has no Parameters section.".Red());
      return false;
    }

    bool ok = true;
    foreach (string name in AppNameKebabParameters)
    {
      string? value = values.TryGetProperty(name, out JsonElement element) && element.ValueKind == JsonValueKind.String
        ? element.GetString()
        : null;
      if (value != expectedKebabName)
      {
        Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} Parameters:{name} is '{value}', expected '{expectedKebabName}' (appNameKebab template symbol).".Red());
        ok = false;
      }
      else if (!IsDnsLabel(value))
      {
        Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} Parameters:{name} '{value}' is not a DNS-1123 label (≤ 63 chars, [a-z0-9-], alphanumeric ends).".Red());
        ok = false;
      }
    }

    if (ok)
      Terminal.WriteLine($"AppHost deploy parameters use the app's kebab name ({expectedKebabName}).");

    return ok;
  }

  /// <summary>
  /// Task 311: the generated AppHost appsettings.json's Parameters:ingress-port — an integer inside the template's ingressPort
  /// generator range and not the monorepo's own value it replaces. Returns the port, or null after printing the failure.
  /// </summary>
  public int? ReadGeneratedIngressPort(string outputDir)
  {
    const string parameterName = "ingress-port";
    string templateJson = Path.Combine(RepoRoot, ".template.config", "template.json");
    int low, high;
    string replaced;
    try
    {
      using var template = JsonDocument.Parse(File.ReadAllText(templateJson), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
      JsonElement symbol = template.RootElement.GetProperty("symbols").GetProperty("ingressPort");
      low = symbol.GetProperty("parameters").GetProperty("low").GetInt32();
      high = symbol.GetProperty("parameters").GetProperty("high").GetInt32();
      replaced = symbol.GetProperty("replaces").GetString() ?? "";
    }
    catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
    {
      Terminal.WriteErrorLine($".template.config/template.json has no usable symbols.ingressPort (parameters.low/high, replaces): {exception.Message}".Red());
      return null;
    }

    string path = Path.Combine(outputDir, AppHostAppSettingsRelativePath.Replace('/', Path.DirectorySeparatorChar));
    string? value = null;
    try
    {
      using var document = JsonDocument.Parse(File.ReadAllText(path));
      if (document.RootElement.TryGetProperty("Parameters", out JsonElement section)
        && section.ValueKind == JsonValueKind.Object
        && section.TryGetProperty(parameterName, out JsonElement element)
        && element.ValueKind == JsonValueKind.String)
      {
        value = element.GetString();
      }
    }
    catch (Exception exception) when (exception is JsonException or IOException)
    {
      Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} could not be read: {exception.Message}".Red());
      return null;
    }

    if (value == replaced)
    {
      Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} Parameters:{parameterName} is still the monorepo's {replaced} (ingressPort template symbol did not run).".Red());
      return null;
    }

    if (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int port) || port < low || port > high)
    {
      Terminal.WriteErrorLine($"{AppHostAppSettingsRelativePath} Parameters:{parameterName} is '{value}', expected an integer in {low}-{high} (ingressPort template symbol).".Red());
      return null;
    }

    Terminal.WriteLine($"AppHost ingress-port is the generated {port} ({low}-{high}).");
    return port;
  }
}
