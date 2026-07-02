#region Purpose
// Wire-format DTO carried by the superhero code-first gRPC contract.
#endregion

#region Design
// ProtoMember numbers ARE the wire contract: never renumber or reuse them; add fields with fresh numbers.
// BirthDate uses DataFormat.WellKnown so it serializes as google.protobuf.Timestamp for cross-platform interop
// (protobuf-net's default DateTime encoding is .NET-only).
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

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
