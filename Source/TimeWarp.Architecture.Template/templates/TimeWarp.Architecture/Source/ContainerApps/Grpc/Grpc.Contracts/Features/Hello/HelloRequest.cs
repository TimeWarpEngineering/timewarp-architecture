namespace TimeWarp.Architecture.Features.Hellos;

using System.Runtime.Serialization;

[DataContract]
public class HelloRequest
{
  [DataMember(Order = 1)]
  public string Name { get; set; } = string.Empty;
}
