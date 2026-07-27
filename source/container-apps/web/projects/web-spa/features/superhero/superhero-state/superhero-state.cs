#region Purpose
// Root partial of the superhero TimeWarp State store: holds the superhero list; actions live in sibling superhero-state.*.cs partials.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[StateAccess]
public sealed partial class SuperheroState : State<SuperheroState>
{
  private readonly List<SuperheroDto> SuperheroList = [];

  public IReadOnlyList<SuperheroDto> Superheros => SuperheroList.AsReadOnly();

  public override void Initialize() { }
}
