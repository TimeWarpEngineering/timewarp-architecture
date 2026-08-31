#region Purpose
// Automated gate: web-server's JS initializer manifest must list Web.Spa after a host build.
#endregion

#region Design
// Task 200: a stale host list omitted web.spa*.lib.module.js and /Login threw "'Spa' was undefined".
// The web-server csproj Error target is the build-time gate; this test reads the generated JSON so
// `dev test` fails even if someone removes the MSBuild assertion. Looks under the web-server
// project obj tree (jsmodules.build.manifest.json and *.modules.json). Fingerprinted names
// (web.spa.{hash}.lib.module.js) match. Host-free — no HostGraph.
#endregion

namespace JsInitializerManifest_;

[TestTag("Build")]
public class HostBuild_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<HostBuild_Given_>();

  public static Task JsmodulesManifest_Should_IncludeWebSpaLibModule()
  {
    string webServerProjectDirectory = Path.Combine(
      FindRepoRoot(),
      "source",
      "container-apps",
      "web",
      "projects",
      "web-server");
    string objDirectory = Path.Combine(webServerProjectDirectory, "obj");
    Directory.Exists(objDirectory).ShouldBeTrue(
      $"web-server obj is missing at {objDirectory}. Build web-server before this gate.");

    List<string> manifests = [];
    foreach (string file in Directory.EnumerateFiles(objDirectory, "*.json", SearchOption.AllDirectories))
    {
      string fileName = Path.GetFileName(file);
      if (fileName.Equals("jsmodules.build.manifest.json", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("jsmodules.publish.manifest.json", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals("web-server.modules.json", StringComparison.OrdinalIgnoreCase))
      {
        manifests.Add(file);
      }
    }

    manifests.Count.ShouldBeGreaterThan(
      0,
      $"No JS initializer manifest under {objDirectory}. Expected jsmodules.build.manifest.json or web-server.modules.json after a web-server build.");

    foreach (string manifest in manifests)
    {
      string json = File.ReadAllText(manifest);
      ContainsWebSpaLibModule(json).ShouldBeTrue(
        $"Host JS initializer list omitted Web.Spa (web.spa*.lib.module.js) in {manifest}: {json}");
    }

    return Task.CompletedTask;
  }

  private static bool ContainsWebSpaLibModule(string json)
  {
    int spa = json.IndexOf("web.spa", StringComparison.OrdinalIgnoreCase);
    if (spa < 0)
    {
      return false;
    }

    int module = json.IndexOf(".lib.module.js", spa, StringComparison.OrdinalIgnoreCase);
    return module > spa;
  }

  private static string FindRepoRoot()
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
}
