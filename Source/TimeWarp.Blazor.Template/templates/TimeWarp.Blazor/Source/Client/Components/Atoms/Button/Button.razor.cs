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
      "font-interMedium",
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

    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment ButtonText { get; set; }
    [Parameter] public RenderFragment SvgIcon { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Default;
    private string CssClass { get; set; }

    public enum ButtonVariant
    {
      Default,
      Outline
    }

    protected async Task OnClickHandler() => await OnClick.InvokeAsync(null);

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
