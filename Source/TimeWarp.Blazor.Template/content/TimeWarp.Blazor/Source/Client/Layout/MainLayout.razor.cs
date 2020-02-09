namespace TimeWarp.Blazor.Layout
{
  using BlazorState.Services;
  using Microsoft.AspNetCore.Components;

  public class MainLayoutBase : LayoutComponentBase
  {
    protected const string HeadingHeight = "52px";
    [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }
  }
}
