namespace TimeWarp.Architecture.Components;

using BlazorState.Services;

public partial class BlazorLocation : BaseComponent
{
  [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }

  public string LocationName => BlazorHostingLocation.IsClientSide ? "Client Side" : "Server Side";
}
