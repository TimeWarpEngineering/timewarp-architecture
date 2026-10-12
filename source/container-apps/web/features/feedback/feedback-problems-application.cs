#region Purpose
// Shared problem details for feedback handlers.
#endregion

#region Design
// Unauthenticated is defense in depth behind [EndpointAuthorize]. A missing item and an item
// owned by someone else share one 404 so a read does not reveal that the id exists.
// StatusUnchanged and Changed are the triage conflicts (task 314): choosing the current status,
// and another writer saving the item between this request's read and write.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using SubmitFeedbackContract = TimeWarp.Architecture.Features.Feedback.SubmitFeedback;

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

  public static SharedProblemDetails AttachmentUnavailable(IReadOnlyCollection<Guid>? unavailableIds = null)
  {
    SharedProblemDetails problem = new()
    {
      Title = "Attachment not available",
      Status = 400,
      Detail = "One or more attachments are no longer available. Pending uploads expire after "
        + $"{FeedbackAttachmentRules.PendingLifetime.TotalHours:0} hours; attach the file again."
    };
    if (unavailableIds is { Count: > 0 })
    {
      problem.Extensions[SubmitFeedbackContract.UnavailableAttachmentIdsExtension] = unavailableIds.ToList();
    }

    return problem;
  }

  public static SharedProblemDetails AlreadyFiled() => new()
  {
    Title = "Attachment already filed",
    Status = 409,
    Detail = "Filed attachments stay with the feedback item."
  };

  public static SharedProblemDetails InvalidStatus() => new()
  {
    Title = "Invalid status",
    Status = 400,
    Detail = "Status must be a defined feedback status other than New."
  };

  public static SharedProblemDetails StatusUnchanged(string status) => new()
  {
    Title = "Status unchanged",
    Status = 409,
    Detail = $"The feedback item is already {status}."
  };

  public static SharedProblemDetails Changed() => new()
  {
    Title = "Feedback changed",
    Status = 409,
    Detail = "Someone else updated this feedback item. Reload it and try again."
  };

  public static SharedProblemDetails InvalidMessage() => new()
  {
    Title = "Invalid message",
    Status = 400,
    Detail = $"A message needs text and at most {FeedbackItem.MaxMessageLength} characters."
  };

  /// <summary>True when <paramref name="body"/> is text the thread can hold.</summary>
  public static bool IsValidMessage(string? body) =>
    !string.IsNullOrWhiteSpace(body) && body.Trim().Length <= FeedbackItem.MaxMessageLength;
}
