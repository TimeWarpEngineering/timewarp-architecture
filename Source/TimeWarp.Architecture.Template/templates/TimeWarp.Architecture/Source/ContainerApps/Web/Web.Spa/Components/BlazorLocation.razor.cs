namespace TimeWarp.Architecture.Components;

using BlazorState.Services;
using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features.Base;

public partial class BlazorLocation : BaseComponent
{
  [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }

  public string LocationName => BlazorHostingLocation.IsClientSide ? "Client Side" : "Server Side";
}
