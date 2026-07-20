#region Purpose
// Endpoint-centric contract for fetching a single role to populate an edit form.
#endregion

#region Design
// RoleId is not declared on the Query: the {RoleId:guid} segment in [ApiRoute] makes the
// source generator emit it on the partial, and the min(1) route constraint replaces a
// FluentValidation rule for it. Response implements IRoleDetails so the edit form binds the
// same shape it submits via UpdateRole. GetMockResponseFactory lets the SPA's
// MockWebApiService serve this endpoint offline with deterministic RoleIds data.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

/// <summary>
/// Get a role by its unique identifier for possible editing.
/// </summary>
[ApiEndpoint]
public static partial class GetRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IAuthApiRequest, IQueryStringRouteProvider, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }

    // GET carries no body, so UserId travels in the query string (manual IAuthApiRequest form —
    // the [AuthApiRequest] attribute's generated helper is not available here).
    public string GetRouteWithQueryString() =>
      $"{GetRoute()}?{this.GetQueryString(new NameValueCollection { { nameof(UserId), UserId.ToString() } })}";
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
