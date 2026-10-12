#region Purpose
// Lists every principal's feedback for administrators, filtered and paged.
#endregion

#region Design
// Status filter: an explicit Status wins; otherwise closed statuses are excluded unless
// IncludeClosed (FeedbackStatusRules.IsClosed is the one copy of "closed"). Enums map by name
// onto the domain. The page window is computed here; the store returns the matching total.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.ListFeedback;
using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;

public sealed class ListFeedback
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IFeedbackStore FeedbackStore;

    public Handler(IFeedbackStore feedbackStore)
    {
      FeedbackStore = feedbackStore;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query request,
      CancellationToken cancellationToken)
    {
      List<DomainStatus>? statuses = null;
      if (request.Status is { } status)
      {
        if (FeedbackContractMap.ToDomain(status) is not { } mapped)
        {
          return FeedbackProblems.InvalidStatus();
        }

        statuses = [mapped];
      }
      else if (!request.IncludeClosed)
      {
        statuses =
        [
          .. Enum.GetValues<ContractStatus>()
            .Where(candidate => !FeedbackStatusRules.IsClosed(candidate))
            .Select(FeedbackContractMap.ToDomain)
            .OfType<DomainStatus>()
        ];
      }

      FeedbackKind? kind = null;
      if (request.Kind is { } requestedKind)
      {
        kind = FeedbackContractMap.ToDomain(requestedKind);
        if (kind is null)
        {
          return new SharedProblemDetails
          {
            Title = "Invalid kind",
            Status = 400,
            Detail = "Kind must be a defined feedback kind."
          };
        }
      }

      int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
      int page = Math.Max(request.Page, 1);
      FeedbackPage result = await FeedbackStore
        .ListAllAsync(
          new FeedbackListFilter(statuses, kind, request.FiledFrom, request.FiledTo, (page - 1) * pageSize, pageSize),
          cancellationToken)
        .ConfigureAwait(false);

      Item[] items =
      [
        .. result.Items.Select(item => new Item(
          item.Id.Value,
          item.OwnerPrincipalId,
          FeedbackContractMap.ToContract(item.Kind),
          FeedbackContractMap.ToContract(item.Status),
          item.Title,
          item.FiledAt))
      ];
      return new Response(result.TotalCount, items, page, pageSize);
    }
  }
}
