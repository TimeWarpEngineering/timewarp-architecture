namespace TimeWarp.Architecture.Features.Sidebars;

using CssBuilder = BlazorComponentUtilities.CssBuilder;
partial class SidebarMobileMenu
{
  //Off-canvas menu backdrop, show/hide based on off-canvas menu state.

  //Entering: "transition-opacity ease-linear duration-300"
  //  From: "opacity-0"
  //  To: "opacity-100"
  //Leaving: "transition-opacity ease-linear duration-300"
  //  From: "opacity-100"
  //  To: "opacity-0"

  private string BackdropClasses =>
    new CssBuilder("fixed inset-0 bg-gray-900/80")
      .AddClass(new CssBuilder("transition-opacity ease-linear duration-300"), true)
      .AddClass("opacity-100", when: SidebarState.IsOpen)
      .AddClass("opacity-0", when: !SidebarState.IsOpen)
      .Build();

  //  Off-canvas menu, show/hide based on off-canvas menu state.

  //  Entering: "transition ease-in-out duration-300 transform"
  //    From: "-translate-x-full"
  //    To: "translate-x-0"
  //  Leaving: "transition ease-in-out duration-300 transform"
  //    From: "translate-x-0"
  //    To: "-translate-x-full"
  private string MenuClasses =>
    new CssBuilder("relative mr-16 flex w-full max-w-xs flex-1")
      .AddClass(new CssBuilder("transition ease-in-out duration-300 transform"), true)
      .AddClass("translate-x-0", when: SidebarState.IsOpen)
      .AddClass("-translate-x-full", when: !SidebarState.IsOpen)
      .Build();

        //<!--
        //  Close button, show/hide based on off-canvas menu state.

        //  Entering: "ease-in-out duration-300"
        //    From: "opacity-0"
        //    To: "opacity-100"
        //  Leaving: "ease-in-out duration-300"
        //    From: "opacity-100"
        //    To: "opacity-0"
        //-->
  private string CloseButtonClasses =>
    new CssBuilder("absolute left-full top-0 flex w-16 justify-center pt-5")
      .AddClass(new CssBuilder("ease-in-out duration-300"), true)
      .AddClass("opacity-100", when: SidebarState.IsOpen)
      .AddClass("opacity-0", when: !SidebarState.IsOpen)
      .Build();

  private async Task CloseSideBar()
  {
    await SidebarState.CloseSideBar();
  }
}
