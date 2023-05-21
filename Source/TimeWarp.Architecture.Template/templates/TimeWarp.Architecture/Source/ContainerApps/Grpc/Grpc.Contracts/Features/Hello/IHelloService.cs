namespace TimeWarp.Architecture.Features.Hellos;

[ServiceContract]
public interface IHelloService
{
  [OperationContract]
  Task<HelloResponse> SayHelloAsync(HelloRequest aHelloRequest, ServerCallContext aCallContext);
}
