namespace TimeWarp.Architecture.Features.ProfileMenus.Components;

partial class ProfileMenuPanel
{
  /// <summary>
  ///  Need to render it while opening and closing to visualize the transitions.
  /// </summary>
  private bool RenderMenuPanel => ProfileMenuState.MenuState != ProfileMenuState.MenuStates.Closed;

  private readonly string BaseClasses = "py-1 bg-white rounded-md shadow-xs ring-1 ring-gray-900/5";

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

  //protected override async Task OnAfterRenderAsync(bool firstRender)
  //{
  //  // We need to subscribe to be notified when the user no longer has interest in the menu.
  //  // How should we do this?

  //  //await base.OnAfterRenderAsync(firstRender);
  //  //if (firstRender)
  //  //{
  //  //  ObjRef = DotNetObjectReference.Create(this);
  //  //}
  //  //if (!WasOpen && ProfileMenuState.IsOpen)
  //  //{
  //  //  await JSRuntime.InvokeVoidAsync("window.NotifyLossOfInterest", nameof(ProfileMenuPanel), ObjRef);
  //  //  WasOpen = ProfileMenuState.IsOpen;
  //  //  StateHasChanged();
  //  //}
  //}

  // We need to Handle the INotification<ProfileMenuState.Close.Action> to close the menu when the user clicks outside of the menu.

}
