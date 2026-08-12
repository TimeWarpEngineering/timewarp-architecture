#region Purpose
// Code-behind for the Superheros page: wires the route and kicks off the gRPC superhero fetch on initialization.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[Page("/Superheros", Policy = Policies.CanViewDeveloperPage)]
[Authorize(Policy = Policies.CanViewDeveloperPage)]
partial class SuperheroPage
{
  protected override async Task OnInitializedAsync() => await SuperheroState.FetchSuperhero();
}
