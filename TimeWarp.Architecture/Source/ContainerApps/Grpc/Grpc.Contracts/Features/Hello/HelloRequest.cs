namespace TimeWarp.Architecture.Features.Hellos;

[DataContract]
public class HelloRequest
{
  [DataMember(Order = 1)]
  public string Name { get; set; } = string.Empty;
}
