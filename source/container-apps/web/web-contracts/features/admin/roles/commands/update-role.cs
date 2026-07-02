#region Purpose
// Endpoint-centric contract for updating a role.
#endregion

#region Design
// The RoleId the Validator references is not declared here: the {RoleId:int} segment in
// [ApiRoute] makes the source generator emit it on the partial Command, keeping route and
// body in one type. IRoleDetails lets the Validator compose the shared RoleDetailsValidator
// so update enforces the same field rules as create. GetMockResponseFactory lets the SPA's
// MockWebApiService serve this endpoint offline; the empty Response preserves OneOf typing.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

public static partial class UpdateRole
{
  [ApiRoute("api/Role/{RoleId:int}", HttpVerb.Put)]
  public sealed partial class Command : IAuthApiRequest, IRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public Guid Guid { get; init; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.RoleId).NotEmpty();
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response;

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ => new Response();
  }
}
