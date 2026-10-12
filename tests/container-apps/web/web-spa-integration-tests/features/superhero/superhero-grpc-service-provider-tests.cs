#region Purpose
// Host-free coverage of SuperheroGrpcServiceProvider's channel construction: path rebasing onto the
// named client's BaseAddress, the HTTP/2-exact request version, and the fail-loud unregistered client.
#endregion

#region Design
// A capturing primary handler stands in for the network and records the request the factory
// pipeline receives, then fails the call (the gRPC client surfaces that as RpcException). The two
// BaseAddress shapes are the two hosts' registrations (task 312): WASM uses the ingress origin plus
// IngressPaths.GrpcPathPrefix; Web.Server uses https+http://grpc-server for service discovery. The
// server shape asserts the captured URI still has the https+http scheme — that is what proves the
// rebasing handler sits OUTSIDE the factory pipeline, where discovery would resolve it — and that
// GrpcWebHandler left HTTP/2 exact (it relaxes the policy only for an https request URI).
#endregion

namespace SuperheroGrpcServiceProvider_;

using Grpc.Core;
using System.Net;
using TimeWarp.Architecture.Features.Superheros;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Configuration;

public class Channel_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Channel_>();

  public static async Task Wasm_Shape_Keeps_The_Ingress_Grpc_Prefix()
  {
    CapturingHandler capturing = new();
    await using ServiceProvider services = BuildServices(new Uri($"https://host{IngressPaths.GrpcPathPrefix}/"), capturing);
    using SuperheroGrpcServiceProvider provider = CreateProvider(services);

    await CallAsync(provider);

    capturing.LastRequest.ShouldNotBeNull();
    Uri requestUri = capturing.LastRequest!.RequestUri!;
    requestUri.Scheme.ShouldBe(Uri.UriSchemeHttps);
    requestUri.Authority.ShouldBe("host");
    requestUri.AbsolutePath.ShouldStartWith($"{IngressPaths.GrpcPathPrefix}/");
    requestUri.AbsolutePath.ShouldEndWith("/GetSuperhero");
  }

  public static async Task Server_Shape_Is_Rebased_Before_Discovery_With_Http2_Exact()
  {
    CapturingHandler capturing = new();
    await using ServiceProvider services = BuildServices(new Uri($"https+http://{ServiceNames.GrpcServiceName}/"), capturing);
    using SuperheroGrpcServiceProvider provider = CreateProvider(services);

    await CallAsync(provider);

    capturing.LastRequest.ShouldNotBeNull();
    HttpRequestMessage request = capturing.LastRequest!;
    request.RequestUri!.Scheme.ShouldBe("https+http");
    request.RequestUri.Host.ShouldBe(ServiceNames.GrpcServiceName);
    request.RequestUri.AbsolutePath.ShouldEndWith("/GetSuperhero");
    request.Version.ShouldBe(HttpVersion.Version20);
    request.VersionPolicy.ShouldBe(HttpVersionPolicy.RequestVersionExact);
  }

  public static async Task Unregistered_Client_Fails_Loudly()
  {
    ServiceCollection serviceCollection = new();
    serviceCollection.AddHttpClient();
    await using ServiceProvider services = serviceCollection.BuildServiceProvider();
    using SuperheroGrpcServiceProvider provider = CreateProvider(services);

    InvalidOperationException exception = Should.Throw<InvalidOperationException>(provider.GetGrpcService);
    exception.Message.ShouldContain(ServiceNames.GrpcServiceName);
  }

  private static ServiceProvider BuildServices(Uri baseAddress, CapturingHandler capturing)
  {
    ServiceCollection serviceCollection = new();
    serviceCollection.AddHttpClient(ServiceNames.GrpcServiceName, client => client.BaseAddress = baseAddress)
      .ConfigurePrimaryHttpMessageHandler(() => capturing);
    return serviceCollection.BuildServiceProvider();
  }

  private static SuperheroGrpcServiceProvider CreateProvider(ServiceProvider services) =>
    new(services.GetRequiredService<IHttpClientFactory>(), services.GetRequiredService<IHttpMessageHandlerFactory>());

  private static async Task CallAsync(SuperheroGrpcServiceProvider provider)
  {
    ISuperheroService service = provider.GetGrpcService();
    await Should.ThrowAsync<RpcException>(() => service.GetSuperheroAsync(new SuperheroRequest { NumberOfHeros = 1 }));
  }

  private sealed class CapturingHandler : HttpMessageHandler
  {
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      LastRequest = request;
      throw new HttpRequestException("Captured; no network in this test.");
    }
  }
}
