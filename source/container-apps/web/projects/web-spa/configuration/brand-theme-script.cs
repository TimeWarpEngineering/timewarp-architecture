#region Purpose
// The classic script that publishes the Fluent seed and exact flag and sets the initial
// light or dark mode on the body before Fluent's startup script.
#endregion

#region Design
// FluentUI 5 reads data-theme-color from the body and reads exact from localStorage
// (fluentui-blazor:theme-settings). There is no body attribute for exact. This script merges
// color, exact, and base onto the stored object and never writes mode, so a stored light or
// dark choice survives a reload and the OS preference is never stored as a choice.
// Task 471: the script also decides the initial body[data-theme], because Fluent's startup runs
// after the prerendered page can paint. A stored "light" or "dark" mode wins; otherwise
// prefers-color-scheme: dark gives "dark" and anything else gives "light". tokens.css keys the
// dark --twe-* values on that attribute, so a dark OS paints dark from the first frame. App.razor
// renders the script as the first child of <body>, so document.body exists and no body content
// is parsed (or painted) before the attribute is set. Fluent then reads the same answer: its
// initializer takes the stored mode, else body[data-theme]. With nothing stored, Fluent's
// system-change listener re-applies its "system" mode, so the page follows an OS change during
// the session. Fluent 5.0.1 has no option to apply the system mode before its own startup.
// The seed is a validated hex color, so the script text has no quote or tag from configuration.
// Product name is not included.
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
      + "  var stored = null;\n"
      + "  try {\n"
      + "    var existing = {};\n"
      + "    var raw = localStorage.getItem(key);\n"
      + "    if (raw) {\n"
      + "      var parsed = JSON.parse(raw);\n"
      + "      if (parsed && typeof parsed === \"object\" && !Array.isArray(parsed)) existing = parsed;\n"
      + "    }\n"
      + "    if (existing.mode === \"dark\" || existing.mode === \"light\") stored = existing.mode;\n"
      + "    existing.color = \"" + seed + "\";\n"
      + "    existing.exact = " + exact + ";\n"
      + "    existing.base = \"brand\";\n"
      + "    localStorage.setItem(key, JSON.stringify(existing));\n"
      + "  } catch (ignored) {}\n"
      + "  var osDark = !!(window.matchMedia && window.matchMedia(\"(prefers-color-scheme: dark)\").matches);\n"
      + "  document.body.setAttribute(\"data-theme\", stored || (osDark ? \"dark\" : \"light\"));\n"
      + "})();\n";
  }
}
