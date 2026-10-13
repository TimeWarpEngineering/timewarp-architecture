#region Purpose
// SetSearch action: change the text the admin Principals list is filtered by.
#endregion

#region Design
// Pure state edit (task 328). The page dispatches FetchPrincipals afterwards; the server filters
// by display name or email. Blank text (empty string) clears the search (null). Searching needs
// admin.profiles.read; the page only offers the box inside an AuthorizeView on it.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

partial class PrincipalState
{
  public static class SetSearchActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(string search)
      {
        Search = search;
      }

      public string Search { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        PrincipalState.Search = string.IsNullOrWhiteSpace(action.Search) ? null : action.Search.Trim();
        return default;
      }
    }
  }
}
