#region Purpose
// Runtime gate: every JS initializer's static import graph must be served (200 + JS content type).
#endregion

#region Design
// Task 240: TimeWarp.State.Plus 12.0.0-beta.3 renamed downloadFile.js → download-file.js (and
// TimeWarp.State kebab-cased Logger.js / Constants.js). A static import 404 rejects the whole
// module, so window.Spa never assigns. Task 200 only asserts the host list contains
// web.spa*.lib.module.js; it does not fetch the module graph. This test boots the in-proc web
// host, reads /web-server.modules.json (HTML Blazor-Web-Initializers fallback), GETs each
// initializer, parses static import specifiers, and asserts root-relative targets return JS.
// Recurses one level so _content package renames fail CI rather than a live share.
#endregion

namespace JsInitializerImportGraph_;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

[TestTag("Build")]
public class Emit_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Emit_Given_>();

  public static Task LibModule_Should_ImportKebabCaseStatePlusAssets()
  {
    string emitPath = Path.Combine(
      FindRepoRoot(),
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "wwwroot",
      "js",
      "web.spa.lib.module.js");
    File.Exists(emitPath).ShouldBeTrue(
      $"web-spa TypeScript emit is missing at {emitPath}. Build web-spa so tsc regenerates the initializer.");

    string emit = File.ReadAllText(emitPath);
    emit.ShouldContain("/_content/TimeWarp.State.Plus/js/download-file.js", Case.Sensitive);
    emit.ShouldNotContain("/_content/TimeWarp.State.Plus/js/downloadFile.js", Case.Sensitive);
    emit.ShouldContain("/_content/TimeWarp.State/js/logger.js", Case.Sensitive);
    emit.ShouldContain("/_content/TimeWarp.State/js/constants.js", Case.Sensitive);
    emit.ShouldNotContain("/_content/TimeWarp.State/js/Logger.js", Case.Sensitive);
    emit.ShouldNotContain("/_content/TimeWarp.State/js/Constants.js", Case.Sensitive);
    return Task.CompletedTask;
  }

  private static string FindRepoRoot() => JsInitializerImportGraph.FindRepoRoot();
}

[TestTag("Integration")]
public class Serve_Given_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Serve_Given_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task InitializerStaticImports_Should_ReturnJavascript()
  {
    List<string> initializerPaths = await LoadInitializerModulePathsAsync(Web.HttpClient);
    initializerPaths.Count.ShouldBeGreaterThan(
      0,
      "Host JS initializer list was empty. Expected /web-server.modules.json or Blazor-Web-Initializers on /.");

    Dictionary<string, string> fetchedBodies = new(StringComparer.Ordinal);
    List<string> rootRelativeImports = [];

    foreach (string initializerPath in initializerPaths)
    {
      string moduleBody = await AssertJavascriptAssetAsync(Web.HttpClient, initializerPath, fetchedBodies);
      foreach (string specifier in JsInitializerImportGraph.ParseStaticImportSpecifiers(moduleBody))
      {
        if (!JsInitializerImportGraph.TryResolveRootRelative(specifier, out string resolved))
        {
          continue;
        }

        rootRelativeImports.Add(resolved);
        string childBody = await AssertJavascriptAssetAsync(Web.HttpClient, resolved, fetchedBodies);
        foreach (string childSpecifier in JsInitializerImportGraph.ParseStaticImportSpecifiers(childBody))
        {
          if (JsInitializerImportGraph.TryResolveRootRelative(childSpecifier, out string grandchild))
          {
            await AssertJavascriptAssetAsync(Web.HttpClient, grandchild, fetchedBodies);
          }
        }
      }
    }

    rootRelativeImports.ShouldContain(
      path => path.Contains("/_content/", StringComparison.OrdinalIgnoreCase),
      "Initializer import graph had no root-relative _content specifiers. The SPA module must import TimeWarp.State / State.Plus assets.");
  }

  private static async Task<List<string>> LoadInitializerModulePathsAsync(HttpClient httpClient)
  {
    using HttpResponseMessage modulesResponse = await httpClient.GetAsync("/web-server.modules.json");
    if (modulesResponse.IsSuccessStatusCode)
    {
      string json = await modulesResponse.Content.ReadAsStringAsync();
      List<string> fromJson = ParseModulesJson(json);
      if (fromJson.Count > 0)
      {
        return fromJson;
      }
    }

    using HttpResponseMessage htmlResponse = await httpClient.GetAsync("/");
    string html = await htmlResponse.Content.ReadAsStringAsync();
    htmlResponse.StatusCode.ShouldBe(HttpStatusCode.OK, html);

    List<string> fromComment = ParseBlazorWebInitializersComment(html);
    if (fromComment.Count > 0)
    {
      return fromComment;
    }

    List<string> fromHtml = [];
    foreach (Match match in JsInitializerImportGraph.LibModuleJsPath().Matches(html))
    {
      string path = JsInitializerImportGraph.ToRequestPath(match.Groups[1].Value);
      if (!fromHtml.Contains(path, StringComparer.OrdinalIgnoreCase))
      {
        fromHtml.Add(path);
      }
    }

    fromHtml.Count.ShouldBeGreaterThan(
      0,
      $"Neither /web-server.modules.json nor / HTML contained JS initializer modules. HTML starts: {html[..Math.Min(html.Length, 400)]}");
    return fromHtml;
  }

  private static List<string> ParseBlazorWebInitializersComment(string html)
  {
    Match match = JsInitializerImportGraph.BlazorWebInitializersComment().Match(html);
    if (!match.Success)
    {
      return [];
    }

    try
    {
      string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups[1].Value));
      return ParseModulesJson(json);
    }
    catch (FormatException)
    {
      return [];
    }
    catch (JsonException)
    {
      return [];
    }
  }

  private static List<string> ParseModulesJson(string json)
  {
    List<string> paths = [];
    using JsonDocument document = JsonDocument.Parse(json);
    CollectModulePaths(document.RootElement, paths);
    return paths;
  }

  private static void CollectModulePaths(JsonElement element, List<string> paths)
  {
    if (element.ValueKind == JsonValueKind.String)
    {
      string? value = element.GetString();
      if (!string.IsNullOrWhiteSpace(value) && value.Contains(".lib.module.js", StringComparison.OrdinalIgnoreCase))
      {
        string path = JsInitializerImportGraph.ToRequestPath(value);
        if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
          paths.Add(path);
        }
      }

      return;
    }

    if (element.ValueKind == JsonValueKind.Array)
    {
      foreach (JsonElement child in element.EnumerateArray())
      {
        CollectModulePaths(child, paths);
      }

      return;
    }

    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (JsonProperty property in element.EnumerateObject())
      {
        CollectModulePaths(property.Value, paths);
      }
    }
  }

  private static async Task<string> AssertJavascriptAssetAsync(
    HttpClient httpClient,
    string requestPath,
    Dictionary<string, string> fetchedBodies)
  {
    if (fetchedBodies.TryGetValue(requestPath, out string? cachedBody) && cachedBody is not null)
    {
      return cachedBody;
    }

    using HttpResponseMessage response = await httpClient.GetAsync(requestPath);
    string body = await response.Content.ReadAsStringAsync();
    response.StatusCode.ShouldBe(
      HttpStatusCode.OK,
      $"{requestPath} returned {(int)response.StatusCode} {response.StatusCode}. Body starts: {body[..Math.Min(body.Length, 240)]}");

    MediaTypeHeaderValue? mediaType = response.Content.Headers.ContentType;
    mediaType.ShouldNotBeNull($"{requestPath} had no Content-Type");
    string? type = mediaType.MediaType;
    type.ShouldNotBeNull($"{requestPath} had no media type");
    (type.Contains("javascript", StringComparison.OrdinalIgnoreCase)
      || type.Contains("ecmascript", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue(
      $"{requestPath} expected a JS content type, got {mediaType}");

    body.Length.ShouldBeGreaterThan(0, $"{requestPath} was empty");
    (!body.TrimStart().StartsWith('<')).ShouldBeTrue(
      $"{requestPath} looks like HTML, not JavaScript");
    fetchedBodies[requestPath] = body;
    return body;
  }
}

internal static partial class JsInitializerImportGraph
{
  [GeneratedRegex(@"^import\s+.+?\s+from\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex ImportFromSpecifier();

  [GeneratedRegex(@"^import\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex SideEffectImportSpecifier();

  [GeneratedRegex(@"from\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
  private static partial Regex FromSpecifier();

  [GeneratedRegex(@"[""']([^""']+\.lib\.module\.js)[""']", RegexOptions.CultureInvariant)]
  public static partial Regex LibModuleJsPath();

  [GeneratedRegex(@"<!--Blazor-Web-Initializers:([A-Za-z0-9+/=]+)-->", RegexOptions.CultureInvariant)]
  public static partial Regex BlazorWebInitializersComment();

  public static IReadOnlyList<string> ParseStaticImportSpecifiers(string source)
  {
    HashSet<string> specifiers = new(StringComparer.Ordinal);
    AddMatches(specifiers, ImportFromSpecifier(), source);
    AddMatches(specifiers, SideEffectImportSpecifier(), source);
    AddMatches(specifiers, FromSpecifier(), source);
    return [.. specifiers];
  }

  public static bool TryResolveRootRelative(string specifier, out string resolved)
  {
    resolved = "";
    if (string.IsNullOrWhiteSpace(specifier)
      || specifier.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
      || specifier.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
      || specifier.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
      || specifier.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (specifier.StartsWith('/'))
    {
      resolved = specifier;
      return true;
    }

    return false;
  }

  public static string ToRequestPath(string path)
  {
    string trimmed = path.Trim();
    if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
      || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
      Uri uri = new(trimmed);
      return uri.AbsolutePath;
    }

    return trimmed.StartsWith('/') ? trimmed : "/" + trimmed.TrimStart('~', '/');
  }

  public static string FindRepoRoot()
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

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }

  private static void AddMatches(HashSet<string> specifiers, Regex regex, string source)
  {
    foreach (Match match in regex.Matches(source))
    {
      if (match.Success && match.Groups.Count > 1)
      {
        string value = match.Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(value))
        {
          specifiers.Add(value);
        }
      }
    }
  }
}
