namespace TimeWarp.Architecture.Features.Superheros;

using ProtoBuf;

[ProtoContract]
public class SuperheroRequest
{
  [ProtoMember(1)]
  public int NumberOfHeros { get; set; }
}
