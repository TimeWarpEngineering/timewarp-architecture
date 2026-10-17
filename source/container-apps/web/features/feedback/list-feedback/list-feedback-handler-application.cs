#region Purpose
// Lists every principal's feedback for administrators, filtered and paged.
#endregion

#region Design
// Status filter: an explicit Status wins; otherwise closed statuses are excluded unless
// IncludeClosed (FeedbackStatusRules.IsClosed is the one copy of "closed"). Enums map by name
// onto the domain. The page window is computed here; the store returns the matching total.
// SortBy and SortDescending pass through to the store, which sorts before it pages.
// Task 336: owner display names come from FeedbackOwnerNames in one batch over the page's
// distinct owners (never per row), and only with the caller's admin.profiles.read.
// Severity null is every severity. Unset is passed through as a real filter (task 378).
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using TimeWarp.Architecture.Features.Feedback.Domain;
using static TimeWarp.Architecture.Features.Feedback.ListFeedback;
using ContractStatus = TimeWarp.Architecture.Features.Feedback.FeedbackStatus;
using DomainSeverity = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackSeverity;
using DomainStatus = TimeWarp.Architecture.Features.Feedback.Domain.FeedbackStatus;

public sealed class ListFeedback
{
  public sealed class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IFeedbackStore FeedbackStore;
    private readonly FeedbackOwnerNames OwnerNames;

    public Handler(IFeedbackStore feedbackStore, FeedbackOwnerNames ownerNames)
    {
      FeedbackStore = feedbackStore;
      OwnerNames = ownerNames;
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

      DomainSeverity? severity = null;
      if (request.Severity is { } requestedSeverity)
      {
        if (FeedbackContractMap.ToDomain(requestedSeverity) is not { } mappedSeverity)
        {
          return FeedbackProblems.InvalidSeverity();
        }

        severity = mappedSeverity;
      }

      int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
      int page = Math.Clamp(request.Page, 1, MaxPage);
      FeedbackPage result = await FeedbackStore
        .ListAllAsync(
          new FeedbackListFilter(
            statuses,
            kind,
            request.FiledFrom,
            request.FiledTo,
            (page - 1) * pageSize,
            pageSize,
            request.SortBy,
            request.SortDescending,
            request.Search,
            severity),
          cancellationToken)
        .ConfigureAwait(false);

      IReadOnlyDictionary<Guid, string> ownerNames = await OwnerNames
        .FindManyAsync(result.Items.Select(static item => item.OwnerPrincipalId), cancellationToken)
        .ConfigureAwait(false);
      Item[] items =
      [
        .. result.Items.Select(item => new Item(
          item.Id.Value,
          item.OwnerPrincipalId,
          FeedbackContractMap.ToContract(item.Kind),
          FeedbackContractMap.ToContract(item.Status),
          item.Title,
          item.FiledAt,
          ownerNames.GetValueOrDefault(item.OwnerPrincipalId),
          FeedbackContractMap.ToContract(item.Severity)))
      ];
      return new Response(result.TotalCount, items, page, pageSize);
    }
  }
}
