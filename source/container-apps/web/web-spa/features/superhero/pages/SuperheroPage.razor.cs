#region Purpose
// Code-behind for the Superheros page: wires the route and kicks off the gRPC superhero fetch on initialization.
#endregion

namespace TimeWarp.Architecture.Pages;

using static TimeWarp.Architecture.Features.Superheros.SuperheroState;

[Page("/Superheros")]
partial class SuperheroPage
{
  protected override async Task OnInitializedAsync() => await SuperheroState.FetchSuperhero();
}
