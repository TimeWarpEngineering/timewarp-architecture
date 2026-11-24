#region Purpose
// Renders /brand.css from BrandOptions so the shell tokens can change without a component style tag.
#endregion

#region Design
// A linked stylesheet, not a <style> element and not an inline style (task 290, tw-blazor-css-strategy).
// Only role custom properties and the logo height are emitted in :root, and the three dark text
// steps in body[data-theme="dark"] (the same selector tokens.css uses; this sheet links after it,
// so it wins). Hue palette keys are never emitted: component CSS reads roles only (task 513).
// tokens.css remains the fallback and the home of neutrals, type, and spacing. Hex values are
// trimmed; anything else throws so a skipped validator cannot inject a declaration.
// Respond hashes the sheet into a strong ETag. Cache-Control is no-cache. If-None-Match uses
// weak comparison (RFC 9110 §13.1.2): "*" matches, and a listed tag matches after a leading
// "W/" is removed. A match is status 304 with no body, so a proxy that weakens the tag on
// the way back still revalidates instead of painting a stale brand.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Text;

/// <summary>The <c>/brand.css</c> document.</summary>
public static class BrandStylesheet
{
  public const string Path = "/brand.css";

  /// <summary>The dark-mode selector tokens.css and this sheet share.</summary>
  public const string DarkSelector = "body[data-theme=\"dark\"]";

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
    Declaration(css, "--twe-primary", tokens.Primary);
    Declaration(css, "--twe-primary-hover", tokens.PrimaryHover);
    Declaration(css, "--twe-primary-active", tokens.PrimaryActive);
    Declaration(css, "--twe-primary-soft", tokens.PrimarySoft);
    Declaration(css, "--twe-on-primary", tokens.OnPrimary);
    Declaration(css, "--twe-secondary", tokens.Secondary);
    Declaration(css, "--twe-secondary-hover", tokens.SecondaryHover);
    Declaration(css, "--twe-secondary-active", tokens.SecondaryActive);
    Declaration(css, "--twe-secondary-bright", tokens.SecondaryBright);
    Declaration(css, "--twe-highlight", tokens.Highlight);
    Declaration(css, "--twe-highlight-soft", tokens.HighlightSoft);
    Declaration(css, "--twe-info", tokens.Info);
    css.Append("  --twe-logo-height: ");
    css.Append(brand.LogoHeight.ToString(CultureInfo.InvariantCulture));
    css.AppendLine("px;");
    css.AppendLine("}");
    css.AppendLine(DarkSelector + " {");
    Declaration(css, "--twe-primary-text", tokens.PrimaryDark);
    Declaration(css, "--twe-secondary-text", tokens.SecondaryDark);
    Declaration(css, "--twe-highlight-text", tokens.HighlightDark);
    css.AppendLine("}");
    return css.ToString();
  }

  /// <summary>
  /// Status and body for one request. <paramref name="ifNoneMatch"/> is the raw If-None-Match header.
  /// </summary>
  public static Response Respond(BrandOptions brand, string? ifNoneMatch) =>
    Revalidate(Render(brand), ifNoneMatch);

  /// <summary>
  /// 200 with <paramref name="body"/>, or 304 when <paramref name="ifNoneMatch"/> matches its tag.
  /// /brand.json and /manifest.json revalidate the same way as this sheet.
  /// </summary>
  public static Response Revalidate(string body, string? ifNoneMatch)
  {
    string entityTag = EntityTag(body);
    if (Matches(ifNoneMatch, entityTag))
    {
      return new Response(304, Body: null, entityTag);
    }

    return new Response(200, body, entityTag);
  }

  /// <summary>Strong entity tag for a document body. The tag is a quoted SHA-256 hex digest.</summary>
  public static string EntityTag(string body)
  {
    ArgumentException.ThrowIfNullOrEmpty(body);
    byte[] hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(body));
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
      if (token == "*")
      {
        return true;
      }

      // RFC 9110 entity-tag: optional case-sensitive "W/" immediately before the quoted tag.
      // "*" is matched above so a non-tag such as W/* does not become a wildcard.
      if (token.StartsWith("W/\"", StringComparison.Ordinal))
      {
        token = token[2..];
      }

      if (string.Equals(token, entityTag, StringComparison.Ordinal))
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
