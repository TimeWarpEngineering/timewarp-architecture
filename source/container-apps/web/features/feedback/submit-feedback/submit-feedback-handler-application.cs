#region Purpose
// Files one feedback item for the authenticated principal and optionally emails a copy.
#endregion

#region Design
// Owner comes from ICurrentPrincipalAccessor, never from the client. Kind maps by enum name onto
// the domain enum. The store write happens before mail. EmailCopySent is true only when the
// profile already has an email and the filer opted in. No profile row is created. The permalink
// in the response is the relative path; the message body uses an absolute URL when the request
// has a base URL.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using TimeWarp.Architecture.Mail;
using static TimeWarp.Architecture.Features.Feedback.SubmitFeedback;
using DomainKind = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackKind;

public sealed class SubmitFeedback
{
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IFeedbackStore FeedbackStore;
    private readonly IProfileEmailLookup ProfileEmailLookup;
    private readonly IEmailSender EmailSender;
    private readonly IAppBaseUrlAccessor AppBaseUrlAccessor;

    public Handler(
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IFeedbackStore feedbackStore,
      IProfileEmailLookup profileEmailLookup,
      IEmailSender emailSender,
      IAppBaseUrlAccessor appBaseUrlAccessor)
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      FeedbackStore = feedbackStore;
      ProfileEmailLookup = profileEmailLookup;
      EmailSender = emailSender;
      AppBaseUrlAccessor = appBaseUrlAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Command request,
      CancellationToken cancellationToken)
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return FeedbackProblems.Unauthenticated();
      }

      if (!Enum.TryParse(request.Kind.ToString(), out DomainKind kind) || !Enum.IsDefined(kind))
      {
        return new SharedProblemDetails
        {
          Title = "Invalid kind",
          Status = 400,
          Detail = "Kind must be a defined feedback kind."
        };
      }

      var item = FeedbackItem.File(
        principalId.Value.Value,
        kind,
        request.Title,
        request.Body,
        DateTimeOffset.UtcNow);
      await FeedbackStore.AddAsync(item, cancellationToken).ConfigureAwait(false);

      string permalink = FeedbackPermalink.For(item.Id.Value);
      bool emailCopySent = false;
      if (request.EmailCopy)
      {
        string? email = await ProfileEmailLookup
          .FindEmailAsync(principalId.Value.Value, cancellationToken)
          .ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(email))
        {
          await EmailSender.SendAsync(
            new EmailMessage(email, $"Feedback {item.Id.Value:D}", BuildBody(item, kind, permalink)),
            cancellationToken).ConfigureAwait(false);
          emailCopySent = true;
        }
      }

      return new Response(
        item.Id.Value,
        permalink,
        request.Kind,
        item.Title,
        item.Body,
        emailCopySent);
    }

    private string BuildBody(FeedbackItem item, DomainKind kind, string permalink)
    {
      Uri? baseUrl = AppBaseUrlAccessor.GetBaseUrl();
      string link = baseUrl is null
        ? permalink
        : baseUrl.AbsoluteUri.TrimEnd('/') + permalink;
      return
        $"""
        Id: {item.Id.Value:D}
        Kind: {kind}
        Title: {item.Title}
        Permalink: {link}

        {item.Body}
        """;
    }
  }
}
