#region Purpose
// Compile-time ClientPipeline membership and host behaviors for the SPA generated mediator.
#endregion

#region Design
// TimeWarp.State 12.0.0-beta.3 scopes store handlers to ClientPipeline (orders 100-400 in the
// library). Host behaviors start at 500. Pre/post pipeline notifications and TrackEvent are
// SPA-owned; ActiveActionBehavior is the Plus opt-in (same as the previous DI registration).
#endregion

[assembly: MediatorScope(typeof(ClientPipeline))]
[assembly: MediatorBehavior(typeof(PrePipelineNotificationRequestPreProcessor<,>), order: 500, Scope = typeof(ClientPipeline))]
[assembly: MediatorBehavior(typeof(PostPipelineNotificationRequestPostProcessor<,>), order: 510, Scope = typeof(ClientPipeline))]
[assembly: MediatorBehavior(typeof(ActiveActionBehavior<,>), order: 520, Scope = typeof(ClientPipeline))]
[assembly: MediatorBehavior(typeof(TrackEventBehavior<,>), order: 530, Scope = typeof(ClientPipeline))]
