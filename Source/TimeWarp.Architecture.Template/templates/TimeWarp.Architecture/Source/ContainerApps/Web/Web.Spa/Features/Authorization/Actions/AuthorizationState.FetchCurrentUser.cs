namespace TimeWarp.Architecture.Features.Authorization;

internal partial class AuthorizationState
{
  public static class FetchCurrentUser
  {
    [TrackAction]
    public sealed class Action
    (
      Guid userId
    ) : IBaseAction
    {
      public Guid UserId { get; } = userId;
    };

    [UsedImplicitly]
    public class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      IPublisher Publisher
    ) : BaseHandler<Action>(store)
    {
      public override async Task Handle(Action action, CancellationToken cancellationToken)
      {
        var query = new GetCurrentUser.Query
        {
          UserId = action.UserId
        };

        OneOf<GetCurrentUser.Response, SharedProblemDetails> apiResponse =
          await webServerApiService.GetResponse<GetCurrentUser.Response>
          (
            query,
            cancellationToken
          );

        apiResponse.Switch
        (
          response =>
          {
            AuthorizationState.ModulesList = response.Modules;
            AuthorizationState.RolesList = response.Roles;
          },
          problemDetails =>
            Publisher.Publish(new NotificationState.AddProblemDetails.Action(problemDetails), cancellationToken)
        );
      }
    }
  }
}
