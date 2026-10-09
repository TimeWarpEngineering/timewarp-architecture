#region Purpose
// Stable identity of one filed feedback item.
#endregion

#region Design
// [TypedId] generates the house Guid surface (Value/New/From, fail-closed JSON) so a feedback
// id cannot be passed where a ProfileId or another aggregate id is required.
// Empty remains unguardable for default(T) — callers use IsEmpty at the edge.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

[TypedId]
public readonly partial record struct FeedbackItemId;
