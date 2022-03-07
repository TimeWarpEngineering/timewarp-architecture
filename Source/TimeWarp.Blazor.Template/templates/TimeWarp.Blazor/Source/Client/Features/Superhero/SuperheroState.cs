namespace TimeWarp.Blazor.Features.Superheros
{
  using BlazorState;
  using System.Collections.Generic;

  internal partial class SuperheroState : State<SuperheroState>
  {
    private List<SuperheroDto> _Superheros;

    public IReadOnlyList<SuperheroDto> Superheros => _Superheros.AsReadOnly();

    public SuperheroState()
    {
      _Superheros = new List<SuperheroDto>();
    }

    public override void Initialize() { }
  }
}
