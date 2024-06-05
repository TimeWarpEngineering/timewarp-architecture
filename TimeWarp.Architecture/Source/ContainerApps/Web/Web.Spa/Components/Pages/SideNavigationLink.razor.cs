namespace TimeWarp.Architecture.Components;

using CssBuilder = BlazorComponentUtilities.CssBuilder;

// TODO use TimeWarp Source Gen and attributes once Chandu gets them finished
//[TwParentComponent]
//[TwAttributeComponent]
public partial class SideNavigationLink : ParentComponent
{
    private readonly string BaseClasses = ""; // TODO Add Bootstrap classes

    private CssBuilder CssBuilder =>
      new BlazorComponentUtilities.CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
}
