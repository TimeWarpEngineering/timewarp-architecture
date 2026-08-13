#region Purpose
// Registers the Superheros route and authorize policy; markup and behavior live in SuperheroPage.razor.
#endregion

#region Design
// Loading is FetchSuperhero [TrackAction]. Superheros is never null (empty list), so a
// null check cannot be a loading signal.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[Page("/Superheros", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class SuperheroPage;
