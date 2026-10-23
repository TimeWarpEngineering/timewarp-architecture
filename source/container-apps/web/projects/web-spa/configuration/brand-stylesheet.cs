#region Purpose
// Renders /brand.css from BrandOptions so the shell tokens can change without a component style tag.
#endregion

#region Design
// A linked stylesheet, not a <style> element and not an inline style (task 290, tw-blazor-css-strategy).
// Only brand custom properties and the logo height are emitted. tokens.css remains the fallback and
// the home of neutrals, type, and spacing. Hex values are trimmed; anything else throws so a skipped
// validator cannot inject a declaration.
// Respond hashes the sheet into a strong ETag. Cache-Control is no-cache. A matching If-None-Match
// is status 304 with no body, so the browser revalidates instead of painting a stale brand.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Text;

/// <summary>The <c>/brand.css</c> document.</summary>
public static class BrandStylesheet
{
  public const string Path = "/brand.css";

  /// <summary>One GET of <see cref="Path"/>: status, optional body, and the strong entity tag.</summary>
  public readonly record struct Response
  (
    int StatusCode,
    string? Body,
    string EntityTag
  )
  {
    /// <summary>Browser must revalidate. The sheet is not a fingerprinted static asset.</summary>
    public const string CacheControl = "no-cache";
  }

  /// <summary>CSS custom properties for one brand. Throws when a color is not hex.</summary>
  public static string Render(BrandOptions brand)
  {
    ArgumentNullException.ThrowIfNull(brand);
    ArgumentNullException.ThrowIfNull(brand.Tokens);
    if (brand.LogoHeight is < 1 or > 512)
    {
      throw new InvalidOperationException("Logo height is outside 1..512.");
    }

    BrandTokenOptions tokens = brand.Tokens;
    StringBuilder css = new();
    css.AppendLine(":root {");
    Declaration(css, "--twe-purple", tokens.Purple);
    Declaration(css, "--twe-purple-2", tokens.Purple2);
    Declaration(css, "--twe-purple-deep", tokens.PurpleDeep);
    Declaration(css, "--twe-purple-soft", tokens.PurpleSoft);
    Declaration(css, "--twe-blue", tokens.Blue);
    Declaration(css, "--twe-blue-bright", tokens.BlueBright);
    Declaration(css, "--twe-blue-deep", tokens.BlueDeep);
    Declaration(css, "--twe-green", tokens.Green);
    Declaration(css, "--twe-green-deep", tokens.GreenDeep);
    Declaration(css, "--twe-green-soft", tokens.GreenSoft);
    Declaration(css, "--twe-primary", tokens.Primary);
    Declaration(css, "--twe-primary-hover", tokens.PrimaryHover);
    Declaration(css, "--twe-primary-active", tokens.PrimaryActive);
    Declaration(css, "--twe-on-primary", tokens.OnPrimary);
    Declaration(css, "--twe-secondary", tokens.Secondary);
    Declaration(css, "--twe-secondary-hover", tokens.SecondaryHover);
    Declaration(css, "--twe-secondary-active", tokens.SecondaryActive);
    Declaration(css, "--twe-highlight", tokens.Highlight);
    Declaration(css, "--twe-info", tokens.Info);
    css.Append("  --twe-logo-height: ");
    css.Append(brand.LogoHeight.ToString(CultureInfo.InvariantCulture));
    css.AppendLine("px;");
    css.AppendLine("}");
    return css.ToString();
  }

  /// <summary>
  /// Status and body for one request. <paramref name="ifNoneMatch"/> is the raw If-None-Match header.
  /// </summary>
  public static Response Respond(BrandOptions brand, string? ifNoneMatch)
  {
    string css = Render(brand);
    string entityTag = EntityTag(css);
    if (Matches(ifNoneMatch, entityTag))
    {
      return new Response(304, Body: null, entityTag);
    }

    return new Response(200, css, entityTag);
  }

  /// <summary>Strong entity tag for a stylesheet body. The tag is a quoted SHA-256 hex digest.</summary>
  public static string EntityTag(string css)
  {
    ArgumentException.ThrowIfNullOrEmpty(css);
    byte[] hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(css));
    return "\"" + Convert.ToHexString(hash) + "\"";
  }

  private static bool Matches(string? ifNoneMatch, string entityTag)
  {
    if (string.IsNullOrEmpty(ifNoneMatch))
    {
      return false;
    }

    foreach (string part in ifNoneMatch.Split(','))
    {
      string token = part.Trim();
      if (token == "*" || string.Equals(token, entityTag, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static void Declaration(StringBuilder css, string name, string value)
  {
    if (!BrandOptions.IsHexColor(value))
    {
      throw new InvalidOperationException(name + " is not a hex color.");
    }

    css.Append("  ");
    css.Append(name);
    css.Append(": ");
    css.Append(value.Trim());
    css.AppendLine(";");
  }
}
