#region Purpose
// The FeedbackItem shape for the contract-agreement tests: wire enums mirrored from domain enums,
// an activity-entry child collection, and responses that wrap the projection in one member.
#endregion

#region Design
// Task 475, the second shape after Roles and Profile. It is a reduced copy of the template's
// feedback slice. It keeps every rule that slice needs and drops the members that add nothing.
// The domain enums use other numeric values than the wire enums, which proves values are not
// compared, only names. FeedbackActivityEntry.AuthorPrincipalId is Guid? against the root's Guid,
// which is read widening (redaction), not [Computed]. Runs through ContractAgreementTestFixture.Test
// like the other shapes.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

internal static class ContractAgreementFeedbackShape
{
  internal const string Group = "feedback";

  internal const string Contracts =
    """
    namespace App.Features.Feedback
    {
      public enum FeedbackKind { Bug = 0, Idea = 1 }
      public enum FeedbackStatus { New = 0, Planned = 1, Closed = 2 }
      public enum FeedbackActivityKind { StatusChange = 0, AdminReply = 1 }

      public interface IFeedbackItemDetails
      {
        FeedbackKind Kind { get; set; }
        string Title { get; set; }
        string Body { get; set; }
      }

      public sealed class FeedbackActivityEntry
      {
        public int Sequence { get; set; }
        public FeedbackActivityKind Kind { get; set; }
        public Guid? AuthorPrincipalId { get; set; }
        public string? Body { get; set; }
        public FeedbackStatus? ToStatus { get; set; }
        public DateTimeOffset At { get; set; }
      }

      public sealed class Attachment
      {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = "";
      }

      public sealed class FeedbackAdminView
      {
        public Guid FeedbackItemId { get; set; }
        public FeedbackKind Kind { get; set; }
        public FeedbackStatus Status { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        [Computed]
        public bool FilerHasEmail { get; set; }
        [Computed]
        public IReadOnlyList<Attachment> Attachments { get; set; } = [];
        public IReadOnlyList<FeedbackActivityEntry> Activity { get; set; } = [];
      }

      public static class SubmitFeedback
      {
        [Operation("feedback.submit", Group = "feedback")]
        public sealed class Command : IFeedbackItemDetails, ICommand<OneOf<Response, SharedProblemDetails>>
        {
          public FeedbackKind Kind { get; set; }
          public string Title { get; set; } = "";
          public string Body { get; set; } = "";
          public bool EmailCopy { get; set; }
          public List<Guid> AttachmentIds { get; set; } = [];
        }
        public sealed class Response { public Guid FeedbackItemId { get; set; } }
      }

      public static class ChangeFeedbackStatus
      {
        [Operation("feedback.change-status", Group = "feedback")]
        [NotAggregate("Writes FeedbackItem through ChangeStatus, a checked transition.")]
        public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
        {
          public Guid FeedbackItemId { get; set; }
          public FeedbackStatus Status { get; set; }
        }
        public sealed class Response { public FeedbackAdminView Item { get; set; } = new(); }
      }

      public static class GetFeedbackForAdmin
      {
        [Operation("feedback.get-for-admin", Group = "feedback")]
        public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>> { public Guid FeedbackItemId { get; set; } }
        public sealed class Response : BaseResponse { public FeedbackAdminView Item { get; set; } = new(); }
      }

      public static class ListMyFeedback
      {
        [Operation("feedback.list-mine", Group = "feedback")]
        public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>> { public string? Search { get; set; } }
        public sealed class Item
        {
          public Guid FeedbackItemId { get; set; }
          public string Title { get; set; } = "";
          public FeedbackStatus Status { get; set; }
        }
        public sealed class Response : BaseResponse
        {
          public IReadOnlyList<Item> Items { get; set; } = [];
          [Computed]
          public bool EmailCopyAvailable { get; set; }
        }
      }
    }
    """;

  internal const string Domain =
    """
    namespace App.Features.Feedback.Domain
    {
      public enum FeedbackKind { Bug = 10, Idea = 11 }
      public enum FeedbackStatus { New = 10, Planned = 11, Closed = 12 }
      public enum FeedbackActivityKind { StatusChange = 10, AdminReply = 11 }

      public readonly record struct FeedbackItemId(Guid Value);

      public sealed class FeedbackActivity
      {
        public int Sequence { get; }
        public FeedbackActivityKind Kind { get; }
        public Guid AuthorPrincipalId { get; }
        public string? Body { get; }
        public FeedbackStatus? ToStatus { get; }
        public DateTimeOffset At { get; }
      }

      public sealed class FeedbackItem : Entity<FeedbackItemId>, IAggregateRoot
      {
        private readonly List<FeedbackActivity> ActivityList = [];
        private FeedbackItem(FeedbackItemId id) : base(id) { }
        public Guid OwnerPrincipalId { get; }
        public FeedbackKind Kind { get; }
        public string Title { get; } = "";
        public string Body { get; } = "";
        public FeedbackStatus Status { get; private set; }
        public IReadOnlyList<FeedbackActivity> Activity => ActivityList;
        public void ChangeStatus(FeedbackStatus status, Guid adminPrincipalId) { Status = status; }
      }
    }
    """;
}
