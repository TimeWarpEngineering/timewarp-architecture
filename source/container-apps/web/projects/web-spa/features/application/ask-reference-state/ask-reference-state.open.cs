#region Purpose
// Open: list the resource types the signed-in user may reference and show the @ picker.
#endregion

#region Design
// Authorization runs here (ResourceReferenceRegistry.PermittedAsync over the current
// AuthenticationState), not in the component, so the permission filter is testable headless.
// The type list is rebuilt on every open, so a sign-in or role change is honoured.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AskReferenceState
{
  public static class OpenActionSet
  {
    public sealed class Action : IBaseAction;

    internal sealed class Handler
    (
      IStore store,
      AuthenticationStateProvider authenticationStateProvider,
      IAuthorizationService authorizationService
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        // One read for this dispatch. The list is rebuilt on the next Open.
#pragma warning disable BL0013
        AuthenticationState authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
        IReadOnlyList<ResourceReferenceType> permitted =
          await ResourceReferenceRegistry.PermittedAsync(authenticationState.User, authorizationService);
        List<ResourceReferenceTypeRow> rows = [];
        foreach (ResourceReferenceType type in permitted)
        {
          rows.Add(type.ToRow());
        }

        AskReferenceState.Types = rows;
        AskReferenceState.IsOpen = true;
        AskReferenceState.SearchGeneration++;
        AskReferenceState.ShowTypes("");
      }
    }
  }
}
