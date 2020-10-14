namespace TimeWarp.Blazor.Components
{
  using BlazorComponentUtilities;

  public partial class HyperLink : ParentComponent
  {
    private readonly string BaseClasses =
      "cursor-pointer underline text-gray-600 hover:text-gray-800 visited:text-purple-600";

    private CssBuilder CssBuilder =>
      new CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
  }
}
