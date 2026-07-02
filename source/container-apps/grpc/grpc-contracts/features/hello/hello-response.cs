#region Purpose
// Code-first gRPC response carrying the Hello sample's greeting back to the caller.
#endregion

namespace TimeWarp.Architecture.Features.Hellos;

[DataContract]
public class HelloResponse
{
  [DataMember(Order = 1)]
  public string Message { get; set; } = string.Empty;
}
