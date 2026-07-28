#region Purpose
// Code-first gRPC request asking the demo superhero service for a batch of fabricated heroes.
#endregion

namespace TimeWarp.Architecture.Features.Superheros;

[ProtoContract]
public class SuperheroRequest
{
  [ProtoMember(1)]
  public int NumberOfHeros { get; set; }
}
