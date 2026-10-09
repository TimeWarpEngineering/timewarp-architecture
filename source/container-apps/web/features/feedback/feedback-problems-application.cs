#region Purpose
// Shared problem details for feedback handlers.
#endregion

#region Design
// Unauthenticated is defense in depth behind [EndpointAuthorize]. A missing item and an item
// owned by someone else share one 404 so a read does not reveal that the id exists.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

internal static class FeedbackProblems
{
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };

  public static SharedProblemDetails NotFound() => new()
  {
    Title = "Feedback not found",
    Status = 404,
    Detail = "This feedback item is not available."
  };
}
