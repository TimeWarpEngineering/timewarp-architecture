namespace BlazorHosted_CSharp.Client.Layout
{
  using BlazorState.Services;
  using Microsoft.AspNetCore.Components;
  using Microsoft.AspNetCore.Components.Layouts;

  public class MainLayoutBase : LayoutComponentBase
  {
    [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }
    
    protected const string HeadingHeight = "52px";
  }
}
