#region Purpose
// Builds the stable relative permalink for a feedback item.
#endregion

#region Design
// The API always returns the path (/Feedback/{guid:D}). Mail prepends the request base URL
// when one is available. The path is the contract between the SPA route and the receipt.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

public static class FeedbackPermalink
{
  public static string For(Guid feedbackItemId) => $"/Feedback/{feedbackItemId:D}";
}
