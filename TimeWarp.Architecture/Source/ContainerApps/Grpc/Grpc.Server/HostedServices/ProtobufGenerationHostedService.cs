namespace TimeWarp.Architecture.HostedServices;

public class ProtobufGenerationHostedService : IHostedService
{
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public ProtobufGenerationHostedService
  (
    IServiceProvider aServiceProvider,
    ILogger<ProtobufGenerationHostedService> aLogger
  )
  {
    ServiceProvider = aServiceProvider;
    Logger = aLogger;
  }

  public async Task StartAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(ProtobufGenerationHostedService)} has started.");

    // TODO automate the generation of these using Reflection

    var schemaGenerator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
    {
      ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
    };

    string schema = schemaGenerator.GetSchema<ISuperheroService>();
    Directory.CreateDirectory("protos");
    File.WriteAllText("protos/superherocservice.proto", schema);

    await Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken aCancellationToken)
  {
    Logger.LogInformation($"{nameof(ProtobufGenerationHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
