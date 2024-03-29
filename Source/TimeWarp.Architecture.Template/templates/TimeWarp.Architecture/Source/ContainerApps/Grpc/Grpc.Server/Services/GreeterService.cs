namespace TimeWarp.Architecture.GrpcServer.Services;

public class GreeterService : Greeter.GreeterBase
{
  private readonly ILogger Logger;
  public GreeterService(ILogger<GreeterService> logger)
  {
    Logger = logger;
  }

  public override Task<HelloReply> SayHello(HelloRequest aHelloRequest, ServerCallContext aServerCallContext) =>
    Task.FromResult(new HelloReply { Message = "Hello " + aHelloRequest.Name });
}
