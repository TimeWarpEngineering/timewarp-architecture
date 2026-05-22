namespace TimeWarp.Architecture.GrpcServer.Services;

public class GreeterService : Greeter.GreeterBase
{
  private readonly ILogger Logger;
  public GreeterService(ILogger<GreeterService> logger)
  {
    Logger = logger;
  }

  public override Task<HelloReply> SayHello(HelloRequest helloRequest, ServerCallContext serverCallContext) =>
    Task.FromResult(new HelloReply { Message = "Hello " + helloRequest.Name });
}
