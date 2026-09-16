#region Purpose
// Marker outcome: bootstrap token is valid and unknown; the host must park claims and offer a choice.
#endregion

#region Design
// ProcessAsync used to mint a principal on unknown-handle bootstrap. That silently forked
// passkey accounts. Returning this instead of PrincipalId tells EntraTicketHttp to park the
// validated claims and redirect to /Login/Microsoft365/Choose. Create and already-have
// complete the parked ticket; the processor never creates a principal on this path.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

public readonly struct EntraChoiceRequired;
