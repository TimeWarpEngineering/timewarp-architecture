namespace TimeWarp.Blazor.Client.Layout
{
  using BlazorState.Services;
  using Microsoft.AspNetCore.Components;
  
  public class MainLayoutBase : LayoutComponentBase
  {
    [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }
    
    protected const string HeadingHeight = "52px";
  }
}
