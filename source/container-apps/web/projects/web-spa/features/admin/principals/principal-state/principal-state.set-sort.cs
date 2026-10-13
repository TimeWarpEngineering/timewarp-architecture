#region Purpose
// SetSort action: change the column and direction the admin Principals list is ordered by.
#endregion

#region Design
// Pure state edit (task 328, the task 327 pattern). The grid's sort click lands here through the
// page, which then dispatches FetchPrincipals; the server sorts the whole list.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

partial class PrincipalState
{
  public static class SetSortActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(PrincipalListSortColumn sortBy, bool sortDescending)
      {
        SortBy = sortBy;
        SortDescending = sortDescending;
      }

      public PrincipalListSortColumn SortBy { get; }
      public bool SortDescending { get; }
    }

    internal class Handler(IStore store) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        PrincipalState.SortBy = action.SortBy;
        PrincipalState.SortDescending = action.SortDescending;
        return default;
      }
    }
  }
}
