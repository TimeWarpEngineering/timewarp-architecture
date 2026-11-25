#region Purpose
// The Ask answer bar's thumbs, which file feedback, compiled only with the postgres template flag.
#endregion

#region Design
// Task 522: the feedback slice ships only in apps with a database (postgres flag), because its
// filings must be kept. The thumbs file a SubmitFeedback through FeedbackState, so their handler
// and CanFileFeedback live here, where a C# preprocessor region compiles them out; the markup
// shows the thumbs only when CanFileFeedback. Razor @code is outside TWA0009 but this file is not,
// so the Applications → Feedback edge is declared on the type.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

#if postgres
[CrossSliceReference(typeof(FeedbackState), "Ask thumbs file the last answer as feedback.")]
#endif
partial class AskAnswerBar
{
#if postgres
  private static bool CanFileFeedback => true;

  private async Task FileAsync(string title)
  {
    string body = LastAnswer() ?? NoAnswer;
    if (body.Length > SubmitFeedback.MaxBodyLength)
    {
      body = body[..SubmitFeedback.MaxBodyLength];
    }

    await FeedbackState.SubmitFeedback(FeedbackKind.Other, title, body);
  }
#else
  private static bool CanFileFeedback => false;

  private static Task FileAsync(string title)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(title);
    return Task.CompletedTask;
  }
#endif
}
