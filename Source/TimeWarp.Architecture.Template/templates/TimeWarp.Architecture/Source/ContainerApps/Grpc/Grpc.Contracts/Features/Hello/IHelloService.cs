namespace TimeWarp.Architecture.Features.Hellos;

using Grpc.Core;
using System.ServiceModel;
using System.Threading.Tasks;

[ServiceContract]
public interface IHelloService
{
  [OperationContract]
  Task<HelloResponse> SayHelloAsync(HelloRequest aHelloRequest, ServerCallContext aCallContext);
}
