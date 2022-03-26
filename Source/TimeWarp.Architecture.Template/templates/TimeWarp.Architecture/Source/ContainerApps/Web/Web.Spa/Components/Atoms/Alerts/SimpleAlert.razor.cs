namespace TimeWarp.Architecture.Components
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;

  public partial class SimpleAlert : ParentComponent
  {
    [Parameter] public string Title { get; set; }

    private readonly string BaseClasses =
      "bg-positive-100 border border-positive-400 text-positive-700 px-4 py-3 rounded relative";

    private CssBuilder CssBuilder =>
      new CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
  }
}
