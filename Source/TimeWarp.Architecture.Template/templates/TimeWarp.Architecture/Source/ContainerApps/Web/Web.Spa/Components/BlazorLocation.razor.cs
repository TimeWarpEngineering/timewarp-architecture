namespace TimeWarp.Architecture.Components;

public partial class BlazorLocation : BaseComponent
{
  [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }

  public string LocationName => BlazorHostingLocation.IsClientSide ? "Client Side" : "Server Side";
}
