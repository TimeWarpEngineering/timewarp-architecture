namespace TimeWarp.Blazor.Client.Components
{
  using BlazorState;
  using BlazorState.Services;
  using Microsoft.AspNetCore.Components;

  public partial class BlazorLocation
  {
    [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }

    public string LocationName => BlazorHostingLocation.IsClientSide ? "Client Side" : "Server Side";
  }
}
