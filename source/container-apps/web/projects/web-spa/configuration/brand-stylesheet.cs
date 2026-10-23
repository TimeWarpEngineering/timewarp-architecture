#region Purpose
// Renders /brand.css from BrandOptions so the shell tokens can change without a component style tag.
#endregion

#region Design
// A linked stylesheet, not a <style> element and not an inline style (task 290, tw-blazor-css-strategy).
// Only brand custom properties and the logo height are emitted. tokens.css remains the fallback and
// the home of neutrals, type, and spacing. Hex values are trimmed; anything else throws so a skipped
// validator cannot inject a declaration.
#endregion

namespace TimeWarp.Architecture.Configuration;

using System.Text;

/// <summary>The <c>/brand.css</c> document.</summary>
public static class BrandStylesheet
{
  public const string Path = "/brand.css";

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
