namespace TimeWarp.Blazor.Features.Superheros
{
  using BlazorState;
  using System.Collections.Generic;
  using TimeWarp.Blazor.Features.SuperheroGrpc;

  internal partial class SuperheroState : State<SuperheroState>
  {
    private List<SuperheroGrpcDto> _Superheros;

    public IReadOnlyList<SuperheroGrpcDto> Superheros => _Superheros.AsReadOnly();

    public SuperheroState()
    {
      _Superheros = new List<SuperheroGrpcDto>();
    }

    public override void Initialize() { }
  }
}
