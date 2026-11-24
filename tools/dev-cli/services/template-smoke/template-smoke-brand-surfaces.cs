#region Purpose
// Template smoke: rebrands a generated app the tw-brand way (configuration and image files only)
// and fails when a template brand mark is left in the app's brand surfaces.
#endregion
#region Design
// Task 513. The sample rebrand follows skill tw-brand: replace the BrandOptions section in
// web-server appsettings.json, add a new logo under wwwroot/images/brand/, overwrite the icon set
// and the link-preview image at their default paths, and delete the template logo. No shell code
// is edited. Then every text file under web-spa/wwwroot (llms.txt, auth.md, index.md, the
// .well-known documents, css, appsettings) and web-server/components/App.razor is searched,
// case-insensitively, for "timewarp" and the template seed #55409c, and every file name for
// "timewarp". /manifest.json and /brand.css are served from BrandOptions, so the file check is
// that wwwroot/manifest.json does not exist; web-server-integration-tests'
// Serve_Given_A_Rebranded_Host_ GETs both documents for a sample brand.
// Allowed, because they are dependency or code names and not the brand: TimeWarp.<Library>
// package and namespace names (not TimeWarp.Architecture, which sourceName rewrites), TimeWarp
// DevTools, PascalCase or camelCase identifiers that start with TimeWarp (TimeWarpPage), the
// TimeWarpEngineering repository and docs URLs of a library, the timewarp-identity agent skill
// (TimeWarp.Identity's), and x-timewarp-* OpenAPI extensions. tokens.css hue palette lines
// (--twe-purple, --twe-blue, --twe-green) may hold the seed hex: that palette only seeds role
// defaults, and /brand.css overrides every role.
// One BrandOptions source: the web-server section exists and the SPA appsettings has none.
// Runs on the generate-only Contoso.Shop output, so it costs no restore or build.
#endregion

namespace DevCli.Services;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

/// <summary>
/// Applies a sample configuration-only rebrand to a generated app and asserts no template brand remains.
/// </summary>
internal sealed partial class TemplateSmokeBrandSurfaces
{
  private const string WebProjects = "source/container-apps/web/projects";
  private const string SampleProductName = "Northwind Traders";
  private const string SampleLogo = "images/brand/northwind-logo.svg";

  private static readonly string[] TemplateBrandFiles =
  [
    "images/timewarp-horizontal.svg",
  ];

  private static readonly string[] OverwrittenImages =
  [
    "favicon-32x32.png",
    "apple-touch-icon.png",
    "icon-192.png",
    "icon-512.png",
    "images/og-image.png",
  ];

  private static readonly HashSet<string> BinaryExtensions =
    new(StringComparer.OrdinalIgnoreCase) { ".png", ".ico", ".jpg", ".jpeg", ".gif", ".webp", ".woff", ".woff2", ".ttf", ".br", ".gz" };

  // 1x1 transparent PNG: stands in for an image exported from the app's own mark.
  private static readonly byte[] SamplePng = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

  private readonly ITerminal Terminal;

  public TemplateSmokeBrandSurfaces(ITerminal terminal)
  {
    Terminal = terminal;
  }

  [GeneratedRegex(
    @"github\.com/TimeWarpEngineering/timewarp-[a-z0-9-]+"
    + @"|timewarpengineering\.github\.io/timewarp-[a-z0-9-]+"
    + @"|TimeWarp\.(?!Architecture\b)[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)*"
    + @"|TimeWarp DevTools"
    + @"|\b[Tt]imeWarp(?=[A-Z])[A-Za-z0-9]*"
    + @"|timewarp-identity"
    + @"|x-timewarp-[a-z0-9-]+",
    RegexOptions.CultureInvariant)]
  private static partial Regex AllowedDependencyNames();

  [GeneratedRegex(@"timewarp|#55409c", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
  private static partial Regex TemplateBrandMark();

  [GeneratedRegex(@"^\s*--twe-(purple|blue|green)[a-z0-9-]*\s*:", RegexOptions.CultureInvariant)]
  private static partial Regex HuePaletteDeclaration();

  /// <summary>Rebrands <paramref name="outputDir"/> by configuration and files, then searches its brand surfaces.</summary>
  public bool AssertRebrandLeavesNoTemplateBrand(string outputDir)
  {
    string projects = Path.Combine(outputDir, WebProjects);
    if (!Directory.Exists(Path.Combine(projects, "web-server")))
    {
      Terminal.WriteLine("Rebrand smoke: no web family in this output; skipped.");
      return true;
    }

    string wwwroot = Path.Combine(projects, "web-spa", "wwwroot");
    if (!AssertOneBrandSource(projects, wwwroot) || !ApplySampleRebrand(projects, wwwroot))
    {
      return false;
    }

    List<string> hits = [];
    int scanned = 0;
    foreach (string file in Directory.EnumerateFiles(wwwroot, "*", SearchOption.AllDirectories))
    {
      scanned++;
      string relative = Path.GetRelativePath(outputDir, file).Replace('\\', '/');
      if (TemplateBrandMark().IsMatch(AllowedDependencyNames().Replace(Path.GetRelativePath(wwwroot, file), "")))
      {
        hits.Add($"{relative} (file name)");
      }

      ScanText(file, relative, hits);
    }

    string app = Path.Combine(projects, "web-server", "components", "App.razor");
    scanned++;
    ScanText(app, Path.GetRelativePath(outputDir, app).Replace('\\', '/'), hits);

    if (File.Exists(Path.Combine(wwwroot, "manifest.json")))
    {
      hits.Add("web-spa/wwwroot/manifest.json exists; the manifest is served from BrandOptions");
    }

    if (hits.Count > 0)
    {
      Terminal.WriteErrorLine($"Rebrand smoke: {hits.Count} template brand mark(s) left after a configuration-only rebrand (skill tw-brand):".Red());
      foreach (string hit in hits)
      {
        Terminal.WriteErrorLine($"  {hit}".Red());
      }

      return false;
    }

    Terminal.WriteLine($"Rebrand smoke: {scanned} files scanned, no template brand after a configuration-only rebrand.");
    return true;
  }

  private static void ScanText(string file, string relative, List<string> hits)
  {
    if (BinaryExtensions.Contains(Path.GetExtension(file)))
    {
      return;
    }

    string text = File.ReadAllText(file);
    if (text.Contains('\0', StringComparison.Ordinal))
    {
      return;
    }

    bool tokens = file.EndsWith("tokens.css", StringComparison.OrdinalIgnoreCase);
    string[] lines = text.Split('\n');
    for (int index = 0; index < lines.Length; index++)
    {
      string line = lines[index];
      if (tokens && HuePaletteDeclaration().IsMatch(line))
      {
        continue;
      }

      string checkedText = AllowedDependencyNames().Replace(line, "");
      if (TemplateBrandMark().IsMatch(checkedText))
      {
        hits.Add($"{relative}:{index + 1}: {line.Trim()}");
      }
    }
  }

  private bool AssertOneBrandSource(string projects, string wwwroot)
  {
    JsonObject? server = ReadJson(Path.Combine(projects, "web-server", "appsettings.json"));
    if (server?["BrandOptions"] is not JsonObject)
    {
      Terminal.WriteErrorLine("Rebrand smoke: web-server/appsettings.json has no BrandOptions section.".Red());
      return false;
    }

    JsonObject? spa = ReadJson(Path.Combine(wwwroot, "appsettings.json"));
    if (spa?["BrandOptions"] is not null)
    {
      Terminal.WriteErrorLine("Rebrand smoke: web-spa/wwwroot/appsettings.json has a BrandOptions copy; the server is the one source.".Red());
      return false;
    }

    return true;
  }

  private bool ApplySampleRebrand(string projects, string wwwroot)
  {
    string appsettings = Path.Combine(projects, "web-server", "appsettings.json");
    JsonObject? settings = ReadJson(appsettings);
    if (settings is null)
    {
      Terminal.WriteErrorLine($"Rebrand smoke: could not read {appsettings}.".Red());
      return false;
    }

    settings["BrandOptions"] = SampleBrandOptions();
    File.WriteAllText(appsettings, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

    string brandDir = Path.Combine(wwwroot, "images", "brand");
    Directory.CreateDirectory(brandDir);
    File.WriteAllText(
      Path.Combine(wwwroot, SampleLogo),
      "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 180 40\"><title>" + SampleProductName
      + "</title><rect width=\"40\" height=\"40\" rx=\"8\" fill=\"#0b6e4f\"/></svg>\n");

    foreach (string image in OverwrittenImages)
    {
      File.WriteAllBytes(Path.Combine(wwwroot, image), SamplePng);
    }

    foreach (string templateFile in TemplateBrandFiles)
    {
      File.Delete(Path.Combine(wwwroot, templateFile));
    }

    return true;
  }

  private static JsonObject SampleBrandOptions() =>
    new()
    {
      ["ProductName"] = SampleProductName,
      ["LogoSrc"] = SampleLogo,
      ["LogoWidth"] = 180,
      ["LogoHeight"] = 40,
      ["SeedColor"] = "#0b6e4f",
      ["ExactRamp"] = false,
      ["Tokens"] = new JsonObject
      {
        ["Primary"] = "#0b6e4f",
        ["PrimaryHover"] = "#11865f",
        ["PrimaryActive"] = "#06492f",
        ["PrimarySoft"] = "#a7e3c8",
        ["Secondary"] = "#c2571a",
        ["SecondaryHover"] = "#d86a2b",
        ["SecondaryActive"] = "#8a3a0d",
        ["SecondaryBright"] = "#ff9a52",
        ["Highlight"] = "#d4a017",
        ["HighlightSoft"] = "#f7e3a1",
        ["Info"] = "#1f6fb2",
        ["PrimaryDark"] = "#7fd8b0",
        ["SecondaryDark"] = "#ffb27d",
        ["HighlightDark"] = "#f2c94c",
      },
    };

  private static JsonObject? ReadJson(string path)
  {
    if (!File.Exists(path))
    {
      return null;
    }

    return JsonNode.Parse(
      File.ReadAllText(path),
      documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject;
  }
}
