#region Purpose
// The shell brand colors BrandOptions writes into /brand.css.
#endregion

#region Design
// Each property is one --twe-* custom property. Defaults match tokens.css so the override sheet
// is a no-op until an operator changes them. Neutrals, type, and status colors stay in tokens.css.
// Names are the token words (Purple2 is --twe-purple-2), not a second palette.
#endregion

namespace TimeWarp.Architecture.Configuration;

/// <summary>Shell brand colors. Unset configuration keeps these TimeWarp defaults.</summary>
public sealed class BrandTokenOptions
{
  public string Purple { get; set; } = "#55409c";
  public string Purple2 { get; set; } = "#6953b1";
  public string PurpleDeep { get; set; } = "#352078";
  public string PurpleSoft { get; set; } = "#c5b4ff";

  public string Blue { get; set; } = "#0085b2";
  public string BlueBright { get; set; } = "#00ccff";
  public string BlueDeep { get; set; } = "#004f78";

  public string Green { get; set; } = "#69a144";
  public string GreenDeep { get; set; } = "#50852c";
  public string GreenSoft { get; set; } = "#d2ffb4";

  public string Primary { get; set; } = "#55409c";
  public string PrimaryHover { get; set; } = "#6953b1";
  public string PrimaryActive { get; set; } = "#352078";
  public string OnPrimary { get; set; } = "#ffffff";

  public string Secondary { get; set; } = "#0085b2";
  public string SecondaryHover { get; set; } = "#009fd6";
  public string SecondaryActive { get; set; } = "#004f78";

  public string Highlight { get; set; } = "#69a144";
  public string Info { get; set; } = "#0085b2";
}
