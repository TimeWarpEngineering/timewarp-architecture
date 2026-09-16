#region Purpose
// Consumes parked Entra claims and mints a new principal (original bootstrap create).
#endregion

#region Design
// Consume the park first (single-use). CompleteBootstrapCreateAsync is the same mint path
// ProcessAsync used to run inline. Issues identity-session and clears the choice cookie.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using TimeWarp.Architecture.Abstractions;
using static TimeWarp.Architecture.Features.Identity.CompleteEntraBootstrapCreate;

public sealed partial class CompleteEntraBootstrapCreate
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IParkedEntraClaimsStore ParkedStore;
    private readonly EntraTicketProcessor Processor;
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IEntraChoiceTicketAccessor ChoiceTicketAccessor;

    public Handler(
      IParkedEntraClaimsStore parkedStore,
      EntraTicketProcessor processor,
      IBrowserSessionService browserSessionService,
      IEntraChoiceTicketAccessor choiceTicketAccessor)
    {
      ParkedStore = parkedStore;
      Processor = processor;
      BrowserSessionService = browserSessionService;
      ChoiceTicketAccessor = choiceTicketAccessor;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      if (!ChoiceTicketAccessor.TryReadParkId(out string parkId)
        || !ParkedStore.TryConsume(parkId, out ParkedEntraClaims? parked)
        || parked is null)
      {
        return IdentityProblems.EntraChoiceExpired();
      }

      ChoiceTicketAccessor.Clear();
      OneOf<PrincipalId, SharedProblemDetails> created =
        await Processor.CompleteBootstrapCreateAsync(parked.Claims, cancellationToken);
      if (created.IsT1)
      {
        return created.AsT1;
      }

      await BrowserSessionService.IssueAsync(created.AsT0, parked.Claims.DisplayName, cancellationToken);
      return new Response(created.AsT0, parked.Destination);
    }
  }
}
