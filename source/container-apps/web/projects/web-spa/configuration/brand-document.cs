#region Purpose
// GET /brand.json: the server's BrandOptions section as a configuration document the
// WebAssembly host loads before it builds its container.
#endregion

#region Design
// Task 513: one BrandOptions source. web-server configuration owns the section; the SPA has no
// appsettings copy. Main calls LoadAsync, which reads this document from the origin that served
// the SPA and adds it as a JSON configuration source, so IOptions<BrandOptions> binds the same
// values the prerendered HTML used and hydration does not swap the mark. A failed read throws:
// the WebAssembly runtime cannot reach its own server, and a silent fallback would paint the
// template brand.
// The body is {"BrandOptions": {...}} written with Utf8JsonWriter (no reflection, trim-safe).
// Every bindable property is written as configured (an empty LogoAlt stays empty, so the SPA
// applies the same fallback as the server). BrandOptions_Should_ binds a rendered document back and compares
// every property, so a new property that is not written here fails that test.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Text;
using System.Text.Json;

/// <summary>The <c>/brand.json</c> document and the WebAssembly loader that reads it.</summary>
public static class BrandDocument
{
  public const string Path = "/brand.json";

  /// <summary>Configuration JSON with one <see cref="BrandOptions.SectionName"/> section.</summary>
  public static string Render(BrandOptions brand)
  {
    ArgumentNullException.ThrowIfNull(brand);
    ArgumentNullException.ThrowIfNull(brand.Tokens);
    using MemoryStream stream = new();
    using (Utf8JsonWriter json = new(stream))
    {
      json.WriteStartObject();
      json.WriteStartObject(BrandOptions.SectionName);
      json.WriteString(nameof(BrandOptions.ProductName), brand.ProductName);
      json.WriteString(nameof(BrandOptions.LogoSrc), brand.LogoSrc);
      json.WriteString(nameof(BrandOptions.LogoAlt), brand.LogoAlt);
      json.WriteNumber(nameof(BrandOptions.LogoWidth), brand.LogoWidth);
      json.WriteNumber(nameof(BrandOptions.LogoHeight), brand.LogoHeight);
      json.WriteString(nameof(BrandOptions.SeedColor), brand.SeedColor);
      json.WriteBoolean(nameof(BrandOptions.ExactRamp), brand.ExactRamp);
      json.WriteString(nameof(BrandOptions.FaviconSrc), brand.FaviconSrc);
      json.WriteString(nameof(BrandOptions.AppleTouchIconSrc), brand.AppleTouchIconSrc);
      json.WriteString(nameof(BrandOptions.Icon192Src), brand.Icon192Src);
      json.WriteString(nameof(BrandOptions.Icon512Src), brand.Icon512Src);
      json.WriteString(nameof(BrandOptions.OgImageSrc), brand.OgImageSrc);
      json.WriteString(nameof(BrandOptions.ThemeColor), brand.ThemeColor);
      json.WriteString(nameof(BrandOptions.BackgroundColor), brand.BackgroundColor);
      WriteTokens(json, brand.Tokens);
      json.WriteEndObject();
      json.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  /// <summary>
  /// Reads <see cref="Path"/> from <paramref name="baseAddress"/> and adds it to
  /// <paramref name="configuration"/>. Throws when the server does not answer 200.
  /// </summary>
  public static async Task LoadAsync
  (
    IConfigurationBuilder configuration,
    Uri baseAddress,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(baseAddress);
    using HttpClient client = new() { BaseAddress = baseAddress };
    byte[] body = await client.GetByteArrayAsync(Path.TrimStart('/'), cancellationToken).ConfigureAwait(false);
    // The JSON provider reads the stream when the source is added; it is not disposed here.
    configuration.AddJsonStream(new MemoryStream(body, writable: false));
  }

  private static void WriteTokens(Utf8JsonWriter json, BrandTokenOptions tokens)
  {
    json.WriteStartObject(nameof(BrandOptions.Tokens));
    json.WriteString(nameof(BrandTokenOptions.Primary), tokens.Primary);
    json.WriteString(nameof(BrandTokenOptions.PrimaryHover), tokens.PrimaryHover);
    json.WriteString(nameof(BrandTokenOptions.PrimaryActive), tokens.PrimaryActive);
    json.WriteString(nameof(BrandTokenOptions.PrimarySoft), tokens.PrimarySoft);
    json.WriteString(nameof(BrandTokenOptions.OnPrimary), tokens.OnPrimary);
    json.WriteString(nameof(BrandTokenOptions.Secondary), tokens.Secondary);
    json.WriteString(nameof(BrandTokenOptions.SecondaryHover), tokens.SecondaryHover);
    json.WriteString(nameof(BrandTokenOptions.SecondaryActive), tokens.SecondaryActive);
    json.WriteString(nameof(BrandTokenOptions.SecondaryBright), tokens.SecondaryBright);
    json.WriteString(nameof(BrandTokenOptions.Highlight), tokens.Highlight);
    json.WriteString(nameof(BrandTokenOptions.HighlightSoft), tokens.HighlightSoft);
    json.WriteString(nameof(BrandTokenOptions.Info), tokens.Info);
    json.WriteString(nameof(BrandTokenOptions.PrimaryDark), tokens.PrimaryDark);
    json.WriteString(nameof(BrandTokenOptions.SecondaryDark), tokens.SecondaryDark);
    json.WriteString(nameof(BrandTokenOptions.HighlightDark), tokens.HighlightDark);
    json.WriteEndObject();
  }
}
