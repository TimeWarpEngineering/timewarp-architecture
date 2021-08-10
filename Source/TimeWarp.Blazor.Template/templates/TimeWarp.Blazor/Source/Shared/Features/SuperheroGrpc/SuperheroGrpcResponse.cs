namespace TimeWarp.Blazor.Features.SuperheroGrpc
{
  using ProtoBuf;
  using System;
  using System.Collections.Generic;
  using System.Runtime.Serialization;

  [DataContract]
  public class SuperheroGrpcResponse
  {
    [DataMember(Order = 1)]
    public List<SuperheroGrpcDto> SuperherosGrpc { get; set; }
    public SuperheroGrpcResponse(){}

  }
}
