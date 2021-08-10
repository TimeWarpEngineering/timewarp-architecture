namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using ProtoBuf;
  using System.Collections.Generic;

  [ProtoContract]
  public class SuperheroGrpcResponse
  {
    [ProtoMember(1)]
    public List<SuperheroGrpcDto> SuperherosGrpc { get; set; }
    public SuperheroGrpcResponse(){}

  }
}
