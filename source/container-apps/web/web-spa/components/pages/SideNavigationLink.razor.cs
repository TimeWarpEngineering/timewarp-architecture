#region Purpose
// Code-behind for a single side-nav link: merges caller-supplied CSS classes into the link via CssBuilder.
#endregion

namespace TimeWarp.Architecture.Components;

using CssBuilder = BlazorComponentUtilities.CssBuilder;

// TODO use TimeWarp Source Gen and attributes once Chandu gets them finished
//[TwParentComponent]
//[TwAttributeComponent]
partial class SideNavigationLink
{
    private readonly string BaseClasses = ""; // TODO Add Bootstrap classes

    private CssBuilder CssBuilder =>
      new BlazorComponentUtilities.CssBuilder(BaseClasses)
      .AddClassFromAttributes(Attributes);
}
