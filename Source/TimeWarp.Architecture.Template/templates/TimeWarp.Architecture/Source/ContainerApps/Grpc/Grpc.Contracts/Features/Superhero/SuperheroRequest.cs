namespace TimeWarp.Architecture.Features.Superheros;

[ProtoContract]
public class SuperheroRequest
{
  [ProtoMember(1)]
  public int NumberOfHeros { get; set; }
}
