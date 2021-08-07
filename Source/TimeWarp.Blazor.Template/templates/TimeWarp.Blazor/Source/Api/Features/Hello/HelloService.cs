namespace TimeWarp.Blazor.Features.Hellos
{
  using System.ServiceModel;
  using System.Threading.Tasks;
  using ProtoBuf.Grpc;

  [ServiceContract]
  public interface IHelloService
  {
    [OperationContract]
    Task<HelloResponse> SayHelloAsync(HelloRequest aHelloRequest, CallContext aCallContext = default);
  }
}
