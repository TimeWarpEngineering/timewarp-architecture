#region Purpose
// Server-to-client chat contract: delivery of a broadcast message to a connected client.
#endregion

#region Design
// Modeled as a mediator Command so an incoming hub message flows through the client's pipeline
// (validation, state updates) like any other request instead of being handled in an ad hoc callback.
// Mirrors SendMessage's shape; the folder (server-to-client) encodes direction because the type alone cannot.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

public static partial class ReceiveMessage
{
    public sealed class Command : IRequest<OneOf<Success, SharedProblemDetails>>
    {
        public string User { get; set; } = null!;
        public string Message { get; set; } = null!;
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.User).NotEmpty();
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
