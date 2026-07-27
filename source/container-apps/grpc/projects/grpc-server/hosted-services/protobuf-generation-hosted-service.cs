#region Purpose
// Emits .proto files from the code-first gRPC contracts so non-.NET clients can generate their own stubs.
#endregion

#region Design
// The code-first contract interfaces are the source of truth; the protos/ output is derived artifact only —
// never hand-edit it or treat it as input.
// Registration is opt-in (its AddHostedService line stays commented in program.cs) because schema export is a
// dev-time task, not a runtime concern.
// Each contract needs an explicit GetSchema<T> call until reflection-based discovery replaces the manual list.
#endregion

namespace TimeWarp.Architecture.HostedServices;

public class ProtobufGenerationHostedService : IHostedService
{
  private readonly IServiceProvider ServiceProvider;
  private readonly ILogger Logger;

  public ProtobufGenerationHostedService
  (
    IServiceProvider serviceProvider,
    ILogger<ProtobufGenerationHostedService> logger
  )
  {
    ServiceProvider = serviceProvider;
    Logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
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

  public Task StopAsync(CancellationToken cancellationToken)
  {
    Logger.LogInformation($"{nameof(ProtobufGenerationHostedService)} has stopped.");
    return Task.CompletedTask;
  }
}
