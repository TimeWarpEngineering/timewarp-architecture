#region Purpose
// Task 205-006: FluentSelect overlay must publish the live WASM identifier SetComboBoxValue.
#endregion

#region Design
// Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.5-26219.1 does not pack
// Components/List/FluentSelect.razor.js; WASM still import()s that URL. Overlay lives in
// web-spa/fluent-ui-overlays and is mapped onto the package _content path via MapStaticAssets.
// Live WASM (rc.4 C# path) invokes Microsoft.FluentUI.Blazor.Select.SetComboBoxValue on the
// imported module; rc.5 C# also calls Components.Select.Initialize. Both trees must be in
// overlay source and the in-proc GET body. Host-free checks: identifiers + SWA endpoints
// list that path after a host build. HTTP check: in-proc web host GET 200 + JS body.
#endregion

namespace FluentSelectJsOverlay_;

using System.Net;
using System.Net.Http.Headers;

[TestTag("Build")]
public class OverlaySource_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<OverlaySource_Given_>();

  public static Task File_Should_ExportLiveSelectIdentifiers()
  {
    string overlayPath = Path.Combine(
      FindRepoRoot(),
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "fluent-ui-overlays",
      "fluent-select.js");
    File.Exists(overlayPath).ShouldBeTrue($"Overlay missing at {overlayPath}");

    string body = File.ReadAllText(overlayPath);
    body.ShouldContain("export var Microsoft");
    body.ShouldContain("Microsoft.FluentUI.Blazor.Select");
    body.ShouldContain("SetComboBoxValue");
    body.ShouldContain("function SetComboBoxValue");
    body.ShouldContain("Microsoft.FluentUI.Blazor.Components.Select");
    body.ShouldContain("Select.Initialize");
    body.ShouldContain("Select.ClearValue");
    body.ShouldContain("function Initialize");
    body.ShouldContain("function ClearValue");
    return Task.CompletedTask;
  }

  public static Task HostStaticWebAssetEndpoints_Should_ListFluentSelectRazorJs()
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

    const string marker = "_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js";
    bool found = false;
    foreach (string file in Directory.EnumerateFiles(objDirectory, "*.json", SearchOption.AllDirectories))
    {
      string fileName = Path.GetFileName(file);
      if (!fileName.Contains("staticwebasset", StringComparison.OrdinalIgnoreCase)
        && !fileName.Contains("endpoint", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string json = File.ReadAllText(file);
      if (json.Contains(marker, StringComparison.Ordinal))
      {
        found = true;
        break;
      }
    }

    found.ShouldBeTrue(
      $"web-server static web asset manifests under {objDirectory} omit {marker}. Rebuild web-spa then web-server so the FluentSelect overlay is in MapStaticAssets.");
    return Task.CompletedTask;
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

  public static async Task ContentUrl_Should_ReturnJavascriptModule()
  {
    const string requestPath = "/_content/Microsoft.FluentUI.AspNetCore.Components/Components/List/FluentSelect.razor.js";
    using HttpResponseMessage response = await Web.HttpClient.GetAsync(requestPath);
    string body = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    MediaTypeHeaderValue? mediaType = response.Content.Headers.ContentType;
    mediaType.ShouldNotBeNull();
    string? type = mediaType.MediaType;
    type.ShouldNotBeNull();
    (type.Contains("javascript", StringComparison.OrdinalIgnoreCase)
      || type.Contains("ecmascript", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue(
      $"Expected a JS content type, got {mediaType}");

    body.Length.ShouldBeGreaterThan(0);
    body.TrimStart().ShouldNotStartWith("<");
    body.ShouldContain("export var Microsoft");
    body.ShouldContain("Microsoft.FluentUI.Blazor.Select");
    body.ShouldContain("SetComboBoxValue");
    body.ShouldContain("Select.Initialize");
    body.ShouldContain("Select.ClearValue");
  }
}
