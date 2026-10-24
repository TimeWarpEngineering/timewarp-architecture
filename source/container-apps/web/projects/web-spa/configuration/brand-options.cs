#region Purpose
// White-label brand bound from configuration: product name, logo, Fluent seed, and shell tokens.
#endregion

#region Design
// Section name "BrandOptions" matches the type name: AddFluentValidatedOptions binds by type name.
// Defaults are the checked-in TimeWarp lockup, so a host with no section paints the same shell.
// The server configuration drives the prerendered HTML (title, data-theme-color, logo, /brand.css).
// The WebAssembly host binds the same section from its own appsettings so hydration does not
// swap the mark. Shell colors stay in tokens.css; /brand.css overrides the brand keys only.
// Fluent reads the seed from the body attribute (task 290). ExactRamp is not a body attribute
// in FluentUI 5, so BrandThemeScript writes it into Fluent's theme storage before blazor.web.js
// and leaves a stored light or dark mode in place. The shell does not call SetThemeAsync.
// An absolute http(s) logo may carry a query or a fragment, so a cache-busted CDN URL is valid.
// A relative or root-relative logo stays a path: ? and # are rejected there.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>
/// Product name, logo, Fluent ramp, and shell token overrides for one generated app.
/// </summary>
public sealed class BrandOptions
{
  public const string SectionName = "BrandOptions";

  public const string DefaultProductName = "TimeWarp.Architecture";
  public const string DefaultLogoSrc = "images/timewarp-horizontal.svg";
  public const string DefaultLogoAlt = "TimeWarp Architecture";
  public const int DefaultLogoWidth = 201;
  public const int DefaultLogoHeight = 48;
  public const string DefaultSeedColor = "#55409c";

  /// <summary>FluentUI theme storage key written before its startup script runs.</summary>
  public const string FluentThemeStorageKey = "fluentui-blazor:theme-settings";

  public string ProductName { get; set; } = DefaultProductName;

  /// <summary>Relative, root-relative, or absolute http(s) logo URL.</summary>
  public string LogoSrc { get; set; } = DefaultLogoSrc;

  public string LogoAlt { get; set; } = DefaultLogoAlt;
  public int LogoWidth { get; set; } = DefaultLogoWidth;
  public int LogoHeight { get; set; } = DefaultLogoHeight;

  /// <summary>
  /// <c>#RGB</c> or <c>#RRGGBB</c> seed for <c>data-theme-color</c>. Fluent builds the component ramp from it.
  /// </summary>
  public string SeedColor { get; set; } = DefaultSeedColor;

  /// <summary>
  /// When true, Fluent's brand step is the seed itself. When false, Fluent builds a harmonized ramp.
  /// </summary>
  public bool ExactRamp { get; set; }

  public BrandTokenOptions Tokens { get; set; } = new();

  /// <summary>Seed color with surrounding whitespace removed, for the attribute, the script, and the sheet.</summary>
  public string NormalizedSeedColor => SeedColor.Trim();

  /// <summary><c>#RGB</c> or <c>#RRGGBB</c>, the same shape Fluent's startup script accepts.</summary>
  public static bool IsHexColor(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    string color = value.Trim();
    if (color.Length is not (4 or 7) || color[0] != '#')
    {
      return false;
    }

    for (int index = 1; index < color.Length; index++)
    {
      if (!Uri.IsHexDigit(color[index]))
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// A logo reference the shell can put in an attribute: a relative path, a root-relative path,
  /// or an absolute http(s) URL. Scheme-relative and script URLs are rejected. A query or a
  /// fragment is allowed only on an absolute http(s) URL.
  /// </summary>
  public static bool IsLogoReference(string? value)
  {
    if (string.IsNullOrWhiteSpace(value) || value.Length > 2048)
    {
      return false;
    }

    if (value.Contains('\\', StringComparison.Ordinal)
      || value.Contains("..", StringComparison.Ordinal)
      || value.Contains(' ', StringComparison.Ordinal)
      || value.Contains('"', StringComparison.Ordinal)
      || value.Contains('\'', StringComparison.Ordinal)
      || value.Contains('<', StringComparison.Ordinal)
      || value.Contains('>', StringComparison.Ordinal))
    {
      return false;
    }

    if (value.StartsWith("//", StringComparison.Ordinal))
    {
      return false;
    }

    // A leading slash is a root-relative path. Uri.TryCreate treats it as a file URI on Unix.
    if (value.StartsWith('/', StringComparison.Ordinal))
    {
      return value.Length > 1
        && !value.Contains('?', StringComparison.Ordinal)
        && !value.Contains('#', StringComparison.Ordinal);
    }

    if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absolute))
    {
      return IsHttpScheme(absolute.Scheme) && string.IsNullOrEmpty(absolute.UserInfo);
    }

    if (value.Contains('?', StringComparison.Ordinal) || value.Contains('#', StringComparison.Ordinal))
    {
      return false;
    }

    return !value.Contains(':', StringComparison.Ordinal);
  }

  /// <summary>Display name safe to place in the document title.</summary>
  public static bool IsProductName(string? value)
  {
    if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
    {
      return false;
    }

    foreach (char character in value)
    {
      if (char.IsControl(character) || character is '<' or '>' or '"' or '\'')
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsHttpScheme(string scheme) =>
    string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
    || string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
}
