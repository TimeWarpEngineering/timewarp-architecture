#region Purpose
// Endpoint-centric contract for deleting a role.
#endregion

#region Design
// [ApiRoute] drives source generation of the route members (hence partial); RoleId comes from
// the {RoleId:guid} route segment, not a hand-declared property.
// The empty Response exists to keep the uniform OneOf<Response, SharedProblemDetails>
// pipeline even though a delete has no payload — callers still get success/problem typing.
// GetMockResponseFactory lets the SPA's MockWebApiService serve this endpoint offline.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

public static partial class DeleteRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Delete)]
  public sealed partial class Command : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
