namespace TimeWarp.Architecture.Features.ProfileMenus.Components;

public partial class ProfileDropDown : BaseComponent, IDisposable
{
  [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

  /// <summary>
  ///  Need to render it while opening and closing to visualize the transitions.
  /// </summary>
  private bool RenderMenuPanel => ProfileMenuState.MenuState != ProfileMenuState.MenuStates.Closed;

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
      .AddClass("opacity-100 scale-100", when: ProfileMenuState.MenuState == ProfileMenuState.MenuStates.Opening)
      .AddClass("opacity-0 scale-95", when: ProfileMenuState.MenuState == ProfileMenuState.MenuStates.Closing);

  private async Task OnClickHandler()
  {
    await Send(new ProfileMenuState.Toggle.Action());
  }

  private DotNetObjectReference<ProfileDropDown> ObjRef;

  protected override void OnInitialized()
  {
    base.OnInitialized();
  }



  [JSInvokable]
  public async Task NotifyLossOfInterest()
  {
    await Send(new ProfileMenuState.Close.Action());
  }

  public override void Dispose()
  {
    base.Dispose();
    try
    {
      if (ObjRef != null)
      {
        ObjRef.Dispose();
        ObjRef = null;
      }
    }
    catch (JSException)
    {
      //swallow the exception
    }
  }
}
