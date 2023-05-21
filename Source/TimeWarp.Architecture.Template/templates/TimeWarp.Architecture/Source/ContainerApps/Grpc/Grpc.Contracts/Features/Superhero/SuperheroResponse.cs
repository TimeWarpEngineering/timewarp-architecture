namespace TimeWarp.Architecture.Features.Superheros;

[ProtoContract]
public class SuperheroResponse
{
  [ProtoMember(1)]
  public List<SuperheroDto>? Superheros { get; set; }
  public SuperheroResponse() { }
}
