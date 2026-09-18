#region Purpose
// Notification that an API call produced SharedProblemDetails, for toast display.
#endregion

#region Design
// Published from DefaultApiHandler/FileResponseApiHandler HandleError so those bases never
// send a toast action (TWS0002). Payload is the problem details object; the toast handler
// decides display and swallows cancellation.
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
