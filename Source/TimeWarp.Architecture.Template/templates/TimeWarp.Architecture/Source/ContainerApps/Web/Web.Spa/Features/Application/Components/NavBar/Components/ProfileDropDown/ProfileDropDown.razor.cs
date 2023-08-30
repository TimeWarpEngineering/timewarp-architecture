namespace TimeWarp.Architecture.Features.Applications.Components.NavBars.Dark;

public partial class ProfileDropDown : BaseComponent
{
  private readonly string BaseClasses = "absolute right-0 z-10 mt-2.5 w-32 origin-top-right rounded-md bg-white shadow-lg ring-1 ring-gray-900/5 focus:outline-none";

  //<!--
  //    TODO:
  //    Profile dropdown panel, show/hide based on dropdown state.

  //    Entering: "transition ease-out duration-100"
  //      From: "opacity-0 scale-95"
  //      To: "opacity-100 scale-100"
  //    Leaving: "transition ease-in duration-75"
  //      From: "opacity-100 scale-100"
  //      To: "opacity-0 scale-95"
  //  -->
  private CssBuilder CssBuilder =>
    new CssBuilder(BaseClasses)
      .AddClass("transition ease-out duration-500")
      //.AddClass("hidden", when: !ProfileMenuState.IsOpen)
      .AddClass("opacity-100 scale-100", when: ProfileMenuState.IsOpen)
      .AddClass("opacity-0 scale-95", when: !ProfileMenuState.IsOpen);

  private async Task OnClickHandler()
  {
    await Send(new ProfileMenuState.ToggleOpen.Action());
  }
}
