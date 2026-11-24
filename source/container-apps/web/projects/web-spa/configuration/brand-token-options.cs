#region Purpose
// The shell brand colors BrandOptions writes into /brand.css, named by role.
#endregion

#region Design
// Each light property is one --twe-* role custom property (PrimaryHover is --twe-primary-hover).
// The three Dark properties are the dark-mode text step of their role: /brand.css writes them
// as --twe-primary-text, --twe-secondary-text, and --twe-highlight-text inside
// body[data-theme="dark"], and tokens.css derives links, accent text, and the focus ring from
// those. A dark variant must read on dark paper (#303030): 4.5:1 for primary and secondary
// (text), 3:1 for highlight (an indicator). Defaults match tokens.css, so the sheet is a no-op
// until an operator changes them. The hue palette (--twe-purple, --twe-blue, --twe-green) is not
// here: it only seeds the role defaults in tokens.css, and component CSS never reads it (task 513).
// Neutrals, type, and status colors stay in tokens.css.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Shell brand colors by role. Unset configuration keeps the template defaults.</summary>
public sealed class BrandTokenOptions
{
  public string Primary { get; set; } = "#55409c";
  public string PrimaryHover { get; set; } = "#6953b1";
  public string PrimaryActive { get; set; } = "#352078";
  public string PrimarySoft { get; set; } = "#c5b4ff";
  public string OnPrimary { get; set; } = "#ffffff";

  public string Secondary { get; set; } = "#0085b2";
  public string SecondaryHover { get; set; } = "#009fd6";
  public string SecondaryActive { get; set; } = "#004f78";
  public string SecondaryBright { get; set; } = "#00ccff";

  public string Highlight { get; set; } = "#69a144";
  public string HighlightSoft { get; set; } = "#d2ffb4";

  public string Info { get; set; } = "#0085b2";

  /// <summary>Primary as text on dark paper. Dark mode's primary text, accent text, and focus ring.</summary>
  public string PrimaryDark { get; set; } = "#c5b4ff";

  /// <summary>Secondary as text on dark paper. Dark mode's links and info text.</summary>
  public string SecondaryDark { get; set; } = "#00ccff";

  /// <summary>Highlight as an indicator on dark paper. Dark mode's shell accent rules.</summary>
  public string HighlightDark { get; set; } = "#69a144";
}
