#region Purpose
// Compile-time unscoped pipeline behaviors for the api-server generated mediator.
#endregion

#region Design
// Order matches the previous DI registration: GenericPipelineBehavior wraps
// FluentValidationBehavior. App behaviors start at 500.
#endregion

[assembly: MediatorBehavior(typeof(GenericPipelineBehavior<,>), order: 500)]
[assembly: MediatorBehavior(typeof(FluentValidationBehavior<,>), order: 510)]
