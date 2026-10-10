#region Purpose
// Stable identity of one feedback attachment.
#endregion

#region Design
// [TypedId] generates the house Guid surface so an attachment id cannot be passed where a
// FeedbackItemId is required. Empty remains unguardable for default(T).
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

[TypedId]
public readonly partial record struct FeedbackAttachmentId;
