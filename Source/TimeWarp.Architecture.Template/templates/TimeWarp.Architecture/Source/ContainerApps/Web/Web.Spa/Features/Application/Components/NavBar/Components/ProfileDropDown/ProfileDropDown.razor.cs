namespace TimeWarp.Architecture.Features.Applications.Components.NavBars.Dark;

public partial class ProfileDropDown : BaseComponent, IDisposable
{
  [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

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
      .AddClass("opacity-0 scale-95 pointer-events-none", when: !ProfileMenuState.IsOpen);

  private async Task OnClickHandler()
  {
    await Send(new ProfileMenuState.ToggleOpen.Action());
  }

  // I need to detect when IsOpen changes from false to true.

  private bool WasOpen = false;

  private DotNetObjectReference<ProfileDropDown> ObjRef;

  protected override void OnInitialized()
  {
    base.OnInitialized();
    WasOpen = ProfileMenuState.IsOpen;
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    if (firstRender)
    {
      ObjRef = DotNetObjectReference.Create(this);
    }
    await base.OnAfterRenderAsync(firstRender);
    if (!WasOpen && ProfileMenuState.IsOpen)
    {
      await JSRuntime.InvokeVoidAsync("window.NotifyLossOfInterest", nameof(ProfileMenuPanel), ObjRef);
      WasOpen = ProfileMenuState.IsOpen;
      StateHasChanged();
    }
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
