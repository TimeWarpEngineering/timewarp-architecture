namespace TimeWarp.Blazor.Components
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;
  using System.Threading.Tasks;

  public partial class Button : DisplayComponent
  {
    private readonly string BaseCss = string.Join
    (
      separator: " ",
      "inline-flex",
      "items-center",
      "px-4",
      "py-2",
      "border",
      "border-transparent",
      "text-sm",
      "leading-5",
      "font-medium",
      "rounded-md",
      "text-white",
      "bg-indigo-600",
      "hover:bg-indigo-500",
      "focus:outline-none",
      "focus:border-indigo-700",
      "focus:shadow-outline-indigo",
      "active:bg-indigo-700",
      "transition",
      "ease-in-out",
      "duration-150"
    );


    private readonly string DefaultCss = string.Join
    (
      separator: " ",
      "bg-primary-400",
      "border-primary-400",
      "hover:bg-primary-600",
      "text-white"
    );

    private readonly string OutlineCss = string.Join
    (
      separator: " ",
      "bg-transparent",
      "border",
      "border-primary-400",
      "hover:border-2",
      "text-primary-500"
    );

    [Parameter] public RenderFragment ButtonText { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment SvgIcon { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Default;
    private string CssClass { get; set; }

    public enum ButtonVariant
    {
      Default,
      Outline
    }

    protected override void OnParametersSet()
    {
      string cssString = Variant == ButtonVariant.Default ? DefaultCss : OutlineCss;
      cssString = BaseCss + " " + cssString;
      CssClass =
        new CssBuilder(cssString)
        .AddClassFromAttributes(Attributes)
        .Build();

      base.OnParametersSet();
    }
  }
}
