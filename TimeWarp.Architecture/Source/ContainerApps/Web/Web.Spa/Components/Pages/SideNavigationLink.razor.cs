namespace TimeWarp.Architecture.Components;
// TODO use TimeWarp Source Gen and attributes once Chandu gets them finished
//[TwParentComponent]
//[TwAttributeComponent]
public partial class SideNavigationLink : ParentComponent
{
    private readonly string BaseClasses = ""; // TODO Add Bootstrap classes

    private CssBuilder CssBuilder =>
      new CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
}
