namespace Tailwind.ApplicationUi.Navigation.Navbars.SimpleDarkWithMenuButtonOnLeft
{
  using BlazorComponentUtilities;
  using Microsoft.AspNetCore.Components;

  public partial class ProfileDropDown : ComponentBase
  {
    private readonly string BaseClasses = "origin-top-right absolute right-0 mt-2 w-48 rounded-md shadow-lg";

    private MenuState State = MenuState.Closed;

    private CssBuilder CssBuilder =>
          new CssBuilder(BaseClasses)
        .AddClass("hidden", when: IsHidden);

    private bool IsHidden => State == MenuState.Closed;

    public enum MenuState
    {
      Open,
      Closed
    }

    private void OnClickHandler() => State = (State == MenuState.Open) ? MenuState.Closed : MenuState.Open;
  }
}
