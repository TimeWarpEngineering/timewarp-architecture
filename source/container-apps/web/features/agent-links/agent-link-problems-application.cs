#region Purpose
// Shared SharedProblemDetails factories for agent-link application handlers.
#endregion

#region Design
// Slice-local (internal static). Unauthenticated is defense-in-depth behind [EndpointAuthorize].
// Forbidden covers kind mismatch (agent hitting approve, human hitting request) and ownership.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks.Application;

internal static class AgentLinkProblems
{
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };

  public static SharedProblemDetails Forbidden(string detail) => new()
  {
    Title = "Forbidden",
    Status = 403,
    Detail = detail
  };

  public static SharedProblemDetails NotFound() => new()
  {
    Title = "Link not found",
    Status = 404,
    Detail = "No such agent-human link."
  };

  public static SharedProblemDetails HumanNotFound() => new()
  {
    Title = "Human not found",
    Status = 404,
    Detail = "No human principal exists with that id."
  };

  public static SharedProblemDetails AlreadyLinked() => new()
  {
    Title = "Link already exists",
    Status = 409,
    Detail = "A pending or approved link already exists for this agent and human."
  };

  public static SharedProblemDetails NotPending() => new()
  {
    Title = "Link is not pending",
    Status = 409,
    Detail = "Only a pending link can be approved or denied."
  };

  public static SharedProblemDetails NotApproved() => new()
  {
    Title = "Link is not approved",
    Status = 409,
    Detail = "humanUx is only available for an approved link."
  };
}
