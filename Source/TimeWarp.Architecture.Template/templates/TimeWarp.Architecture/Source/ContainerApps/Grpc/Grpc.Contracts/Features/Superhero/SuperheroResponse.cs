namespace TimeWarp.Architecture.Features.Superheros;

using ProtoBuf;
using System.Collections.Generic;

[ProtoContract]
public class SuperheroResponse
{
  [ProtoMember(1)]
  public List<SuperheroDto>? Superheros { get; set; }
  public SuperheroResponse() { }

}
