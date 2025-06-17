namespace TimeWarp.Architecture.Features.Superheros;

[ProtoContract]
public class SuperheroResponse
{
  [ProtoMember(1)]
  public required List<SuperheroDto> Superheros { get; init; }
}
