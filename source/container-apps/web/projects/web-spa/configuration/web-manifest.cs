#region Purpose
// GET /manifest.json: the PWA web app manifest built from BrandOptions.
#endregion

#region Design
// Task 513: the manifest is served, not a static wwwroot file, so its name, colors, and icons
// follow the configured brand. name and short_name are ProductName. theme_color and
// background_color are BrandOptions.EffectiveThemeColor and EffectiveBackgroundColor (seed and light background
// when unset). Icons are Icon512Src and Icon192Src; the type is named only for an extension
// this file knows (png, svg, webp), and left out otherwise so the browser sniffs it.
// Written with Utf8JsonWriter (no reflection). Values are validated at startup; the writer
// escapes them, so a value cannot break out of its string.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Text;
using System.Text.Json;

/// <summary>The <c>/manifest.json</c> document.</summary>
public static class WebManifest
{
  public const string Path = "/manifest.json";

  public const string ContentType = "application/manifest+json";

  /// <summary>Manifest JSON for one brand.</summary>
  public static string Render(BrandOptions brand)
  {
    ArgumentNullException.ThrowIfNull(brand);
    using MemoryStream stream = new();
    using (Utf8JsonWriter json = new(stream, new JsonWriterOptions { Indented = true }))
    {
      json.WriteStartObject();
      json.WriteString("name", brand.ProductName);
      json.WriteString("short_name", brand.ProductName);
      json.WriteString("start_url", "./");
      json.WriteString("display", "standalone");
      json.WriteString("background_color", brand.EffectiveBackgroundColor);
      json.WriteString("theme_color", brand.EffectiveThemeColor);
      json.WriteBoolean("prefer_related_applications", false);
      json.WriteStartArray("icons");
      Icon(json, brand.Icon512Src, "512x512");
      Icon(json, brand.Icon192Src, "192x192");
      json.WriteEndArray();
      json.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  private static void Icon(Utf8JsonWriter json, string src, string sizes)
  {
    json.WriteStartObject();
    json.WriteString("src", src);
    if (ImageType(src) is { } type)
    {
      json.WriteString("type", type);
    }

    json.WriteString("sizes", sizes);
    json.WriteEndObject();
  }

  private static string? ImageType(string src)
  {
    string path = src.Split('?', '#')[0];
    if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    {
      return "image/png";
    }

    if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
    {
      return "image/svg+xml";
    }

    return path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp" : null;
  }
}
