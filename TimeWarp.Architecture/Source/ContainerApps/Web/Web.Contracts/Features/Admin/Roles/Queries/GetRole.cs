namespace TimeWarp.Architecture.Features.Admin.Roles;

using Authorization;

/// <summary>
/// Get a role by its unique identifier for possible editing.
/// </summary>
public static partial class GetRole
{
  [RouteMixin("api/Roles/{RoleId:min(1)}", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
    }
  }

  public sealed class Response : IRoleDetails
  {
    public Guid RoleId { get; }
    public string Name { get; set; }
    public string Description { get; set; }

    public Response
    (
      Guid roleId,
      string name,
      string description
    )
    {
      RoleId = Guard.Against.NullOrEmpty(roleId);
      Name = Guard.Against.NullOrEmpty(name);
      Description = Guard.Against.NullOrEmpty(description);
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory()
  {
    return _ =>
      new Response
      (
        roleId: RoleIds.Administrator,
        name: nameof(RoleIds.Administrator),
        description: "The Administrator role is for administrators. And has access to all modules."
      );
  }
}
