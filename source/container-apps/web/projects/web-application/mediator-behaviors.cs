#region Purpose
// Compile-time unscoped pipeline behaviors for the web-application generated mediator.
#endregion

#region Design
// Generators run in this assembly (not web-server) so AddGeneratedMediator is not CS0121 with
// Web.Spa's identically named DI extension. FluentValidationBehavior lives in
// Foundation.Contracts; app behaviors start at 500. PasskeySignInMethodGateBehavior is 400 so a
// disabled passkey ceremony is refused before validation and before the handler.
#endregion

[assembly: MediatorBehavior(typeof(PasskeySignInMethodGateBehavior<,>), order: 400)]
[assembly: MediatorBehavior(typeof(FluentValidationBehavior<,>), order: 500)]
