#region Purpose
// Code-behind for the Superheros page: wires the route and kicks off the gRPC superhero fetch on initialization.
#endregion

#region Design
// Loading is FetchSuperhero [TrackAction]. Superheros is never null (empty list), so a
// null check cannot be a loading signal.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[Page("/Superheros", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class SuperheroPage
{
  private bool IsLoading =>
    IsAnyActive(typeof(SuperheroState.FetchSuperheroActionSet.Action));

  protected override async Task OnInitializedAsync() => await SuperheroState.FetchSuperhero();
}
