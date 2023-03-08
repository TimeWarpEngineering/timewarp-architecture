namespace TimeWarp.Architecture.Features.Superheros;

[TwBaseSpa]
internal partial class SuperheroState : State<SuperheroState>
{
  private readonly List<SuperheroDto> _Superheros;

  public IReadOnlyList<SuperheroDto> Superheros => _Superheros.AsReadOnly();

  public SuperheroState()
  {
    _Superheros = new List<SuperheroDto>();
  }

  public override void Initialize() { }
}
