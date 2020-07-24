namespace TimeWarp.Blazor.Components
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;
  using System;
  public partial class Stack: ParentComponent
  {
    private readonly string BaseCss = "flex";

    private readonly string HorizontalCss = "flex-row";

    private readonly string VerticalCss = string.Join
    (
      separator: " ",
      "flex-col"
    );

    [Parameter] public StackVariant Variant { get; set; } = StackVariant.Horizontal;

    [Parameter] public bool Wrap { get; set; }

    protected string CssClass { get; set; }

    public enum StackVariant
    {
      Horizontal,
      Vertical
    }

    protected override void OnParametersSet()
    {
      Console.WriteLine("OnParametersSet.1");
      string cssString = Variant == StackVariant.Horizontal ? HorizontalCss : VerticalCss;
      cssString = BaseCss + " " + cssString;
      CssClass =
        new CssBuilder(cssString)
        .AddClass("flex-wrap", Wrap)
        .AddClassFromAttributes(Attributes)
        .Build();

      base.OnParametersSet();
    }
  }
}
