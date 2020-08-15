namespace TimeWarp.Blazor.Design.Components
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;
  using TimeWarp.Blazor.Components;

  public partial class ColorBar : DisplayComponent
  {
    private readonly string BaseCss =
      string.Join
      (
        separator: " ",
        "font-Medium",
        "h-10",
        "inline-flex",
        "justify-center",
        "items-center",
        "px-4",
        "py-2",
        "rounded",
        "text-sm",
        "uppercase",
        "width-100"
      );

    [Parameter] public string TailwindColor { get; set; }

    private string CssClass { get; set; }

    protected override void OnParametersSet()
    {
      CssClass =
        new CssBuilder(BaseCss)
        .AddClass("")
        .AddClassFromAttributes(Attributes)
        .Build();

      base.OnParametersSet();
    }
  }
}
