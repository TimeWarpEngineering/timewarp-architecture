namespace TimeWarp.Architecture.Features.Superheros;

[StateAccess]
public sealed partial class SuperheroState : State<SuperheroState>
{
  private readonly List<SuperheroDto> SuperheroList = [];

  public IReadOnlyList<SuperheroDto> Superheros => SuperheroList.AsReadOnly();

  public override void Initialize() { }
}
