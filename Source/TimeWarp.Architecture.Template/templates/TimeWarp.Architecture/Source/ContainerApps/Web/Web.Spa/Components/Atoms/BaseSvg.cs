namespace TimeWarp.Architecture.Components;

using Microsoft.AspNetCore.Components;

public partial class BaseSvg : DisplayComponent
{
  [Parameter] public string FillColor { get; set; }

  [Parameter] public int Size { get; set; } = 16;
}
