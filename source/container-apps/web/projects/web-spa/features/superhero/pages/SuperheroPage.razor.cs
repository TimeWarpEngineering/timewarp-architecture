#region Purpose
// Code-behind for the Superheros page: wires the route and kicks off the gRPC superhero fetch on initialization.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[Page("/Superheros", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class SuperheroPage
{
  protected override async Task OnInitializedAsync() => await SuperheroState.FetchSuperhero();
}
