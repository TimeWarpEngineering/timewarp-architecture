namespace TimeWarp.Blazor.Features.Design.Components
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;
  using TimeWarp.Blazor.Components;

  public partial class ColorBox : DisplayComponent
  {
    private readonly string BaseCss =
      "px-3 py-4 text-sm flex-1 font-semibold leading-tight";

    [Parameter] public int ColorIndex { get; set; }
    [Parameter] public string TailwindColor { get; set; }

    private string ColorClass => $"bg-{TailwindColor.ToLowerInvariant()}-{ColorNumber}";
    private string TextColor => ColorIndex > 4 ? "text-white" : "text-black";
    private int ColorNumber => ColorIndex == 0 ? 50 : ColorIndex * 100;
    private string CssClass { get; set; }

    protected override void OnParametersSet()
    {
      CssClass =
        new CssBuilder(BaseCss)
        .AddClass(ColorClass)
        .AddClass(TextColor)
        .AddClassFromAttributes(Attributes)
        .Build();

      base.OnParametersSet();
    }
  }
}
