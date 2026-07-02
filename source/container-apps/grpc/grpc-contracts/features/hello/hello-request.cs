#region Purpose
// Code-first gRPC request for the Hello sample; DataContract member ordering defines the wire shape in place of a .proto file.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[DataContract]
public class HelloRequest
{
  [DataMember(Order = 1)]
  public string Name { get; set; } = string.Empty;
}
