#region Purpose
// Shared problem details for feedback handlers.
#endregion

#region Design
// Unauthenticated is defense in depth behind [EndpointAuthorize]. A missing item and an item
// owned by someone else share one 404 so a read does not reveal that the id exists.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;

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

  public static SharedProblemDetails EmptyFile() => new()
  {
    Title = "Empty file",
    Status = 400,
    Detail = "The file has no bytes."
  };

  public static SharedProblemDetails TooLarge() => new()
  {
    Title = "File too large",
    Status = 413,
    Detail = "The file is larger than the attachment limit."
  };

  public static SharedProblemDetails UnsupportedMedia() => new()
  {
    Title = "Unsupported file",
    Status = 415,
    Detail = "The file type is not an allowed attachment."
  };

  public static SharedProblemDetails InvalidFileName() => new()
  {
    Title = "Invalid file name",
    Status = 400,
    Detail = "The file name cannot be stored."
  };

  public static SharedProblemDetails TooManyAttachments() => new()
  {
    Title = "Too many attachments",
    Status = 409,
    Detail = $"A feedback item can include at most {FeedbackAttachment.MaxPerItem} attachments."
  };

  public static SharedProblemDetails AttachmentUnavailable() => new()
  {
    Title = "Attachment not available",
    Status = 400,
    Detail = "One of the attachments cannot be filed with this item."
  };

  public static SharedProblemDetails AlreadyFiled() => new()
  {
    Title = "Attachment already filed",
    Status = 409,
    Detail = "Filed attachments stay with the feedback item."
  };
}
