#region Purpose
// Placeholder marking where the to-do feature's client-side state store belongs.
#endregion

#region Design
// Deliberately empty: it neither extends State<T> nor gets registered, because the
// to-do pages and components talk to the API directly rather than through a store.
// Convert it to a TimeWarp State store when the feature needs shared client state.
#endregion

namespace TimeWarp.Architecture.Features.ToDo;

public class TodoState
{
  
}
