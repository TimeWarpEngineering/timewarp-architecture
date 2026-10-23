#region Purpose
// The classic script that publishes the Fluent seed and exact flag before Fluent's startup script.
#endregion

#region Design
// FluentUI 5 reads data-theme-color from the body and reads exact from localStorage
// (fluentui-blazor:theme-settings). There is no body attribute for exact. This script runs
// before blazor.web.js, merges color, exact, and base onto the stored object, and does not
// set mode, so a stored light or dark choice survives a reload. The seed is a validated hex
// color, so the script text has no quote or tag from configuration. Product name is not included.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>JavaScript that runs once in the document, before Fluent builds the ramp.</summary>
public static class BrandThemeScript
{
  /// <summary>Script body with no surrounding script element. Throws when the seed is not hex.</summary>
  public static string Render(BrandOptions brand)
  {
    ArgumentNullException.ThrowIfNull(brand);
    if (!BrandOptions.IsHexColor(brand.SeedColor))
    {
      throw new InvalidOperationException("Brand seed color is not a hex color.");
    }

    string exact = brand.ExactRamp ? "true" : "false";
    string seed = brand.NormalizedSeedColor;
    return
      "(function () {\n"
      + "  var key = \"" + BrandOptions.FluentThemeStorageKey + "\";\n"
      + "  try {\n"
      + "    var existing = {};\n"
      + "    var raw = localStorage.getItem(key);\n"
      + "    if (raw) {\n"
      + "      var parsed = JSON.parse(raw);\n"
      + "      if (parsed && typeof parsed === \"object\" && !Array.isArray(parsed)) existing = parsed;\n"
      + "    }\n"
      + "    existing.color = \"" + seed + "\";\n"
      + "    existing.exact = " + exact + ";\n"
      + "    existing.base = \"brand\";\n"
      + "    localStorage.setItem(key, JSON.stringify(existing));\n"
      + "  } catch (ignored) {}\n"
      + "})();\n";
  }
}
