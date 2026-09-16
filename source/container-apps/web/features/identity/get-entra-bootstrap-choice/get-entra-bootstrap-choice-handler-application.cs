#region Purpose
// Peeks parked Entra bootstrap claims for the choose page without consuming them.
#endregion

#region Design
// TryGet (not TryConsume) so rendering the page does not burn the ticket. Missing/expired
// cookie or park returns Valid=false so the SPA can show "session expired".
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.GetEntraBootstrapChoice;

public sealed partial class GetEntraBootstrapChoice
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IParkedEntraClaimsStore ParkedStore;
    private readonly IEntraChoiceTicketAccessor ChoiceTicketAccessor;

    public Handler(IParkedEntraClaimsStore parkedStore, IEntraChoiceTicketAccessor choiceTicketAccessor)
    {
      ParkedStore = parkedStore;
      ChoiceTicketAccessor = choiceTicketAccessor;
    }

    public Task<OneOf<Response, SharedProblemDetails>> Handle(Query query, CancellationToken cancellationToken)
    {
      if (!ChoiceTicketAccessor.TryReadParkId(out string parkId)
        || !ParkedStore.TryGet(parkId, out ParkedEntraClaims? parked)
        || parked is null)
      {
        return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(valid: false, "/"));
      }

      return Task.FromResult<OneOf<Response, SharedProblemDetails>>(new Response(valid: true, parked.Destination));
    }
  }
}
