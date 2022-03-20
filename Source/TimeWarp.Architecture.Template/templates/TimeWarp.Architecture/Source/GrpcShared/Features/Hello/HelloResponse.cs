namespace TimeWarp.Architecture.Features.Hellos;
using System.Runtime.Serialization;

[DataContract]
public class HelloResponse
{
  [DataMember(Order = 1)]
  public string Message { get; set; } = string.Empty;
}
