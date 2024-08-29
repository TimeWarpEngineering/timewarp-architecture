namespace TimeWarp.Architecture.Features.Profiles;

using static GetProfileData;

partial class ProfileState
{
  internal static class FetchProfileDataActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    [UsedImplicitly]
    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ISender sender,
        ILogger<Handler> logger,
        IValidator<Query>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, sender, logger, validator, authenticationStateProvider) {}

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query());
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        ProfileState.Alias = response.Alias;
        ProfileState.Avatar = response.Avatar;
        return Task.CompletedTask;
      }
    }
  }
}
