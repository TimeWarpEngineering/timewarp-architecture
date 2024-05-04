namespace TimeWarp.Architecture.Features.Authentication;

public static partial class GetCurrentUser
{
  [UsedImplicitly]
  [RouteMixin("api/GetCurrentUser", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
  }

  public sealed class Validator : AbstractValidator<Query>
  {
    public Validator()
    {
      RuleFor(x => x.UserId).NotEmpty().NotEqual(Guid.Empty);
    }
  }

  public sealed class Response
  (
    List<Guid> modules,
    List<Guid> roles
  )
  {
    /// <summary>
    /// List of Module Ids the current user has access to.
    /// </summary>
    /// <remarks> Should be from the ModuleIds</remarks>
    public List<Guid> Modules { get; init; } = modules;


    /// <summary>
    /// List of Roles to which the current user belongs
    /// </summary>
    /// <remarks>Should be from RoleIds</remarks>
    public List<Guid> Roles { get; init; } = roles;
  }
}
