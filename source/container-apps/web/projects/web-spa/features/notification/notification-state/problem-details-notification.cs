#region Purpose
// Notification that an API call produced SharedProblemDetails, for a shell message bar.
#endregion

#region Design
// Published from DefaultApiHandler/FileResponseApiHandler HandleError so those bases never
// send an action (TWS0002). Payload is the problem details object; the message-bar handler
// records it and ignores cancellation.
#endregion

namespace TimeWarp.Architecture.Features;

public sealed class ProblemDetailsNotification : INotification
{
  public ProblemDetailsNotification(SharedProblemDetails sharedProblemDetails)
  {
    SharedProblemDetails = sharedProblemDetails;
  }

  public SharedProblemDetails SharedProblemDetails { get; }
}
