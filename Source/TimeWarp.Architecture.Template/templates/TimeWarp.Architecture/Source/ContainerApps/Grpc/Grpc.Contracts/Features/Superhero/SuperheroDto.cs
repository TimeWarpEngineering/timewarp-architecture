namespace TimeWarp.Architecture.Features.Superheros;

using ProtoBuf;
using System;

[ProtoContract]
public class SuperheroDto
{
  [ProtoMember(1)]
  public string? Id { get; set; }
  [ProtoMember(2)]
  public string? Name { get; set; }
  [ProtoMember(3)]
  public string? Power { get; set; }
  [ProtoMember(4)]
  public int Age { get; set; }
  [ProtoMember(5, DataFormat = DataFormat.WellKnown)]
  public DateTime BirthDate { get; set; }

}
