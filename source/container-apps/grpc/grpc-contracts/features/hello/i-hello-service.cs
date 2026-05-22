namespace TimeWarp.Architecture.Features.Hellos;

[ServiceContract]
public interface IHelloService
{
  [OperationContract]
  Task<HelloResponse> SayHelloAsync(HelloRequest helloRequest, ServerCallContext callContext);
}
