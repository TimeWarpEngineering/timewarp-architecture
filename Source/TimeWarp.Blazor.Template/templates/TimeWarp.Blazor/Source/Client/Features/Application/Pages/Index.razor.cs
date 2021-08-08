namespace TimeWarp.Blazor.Pages
{
  using MediatR;
  using Microsoft.AspNetCore.Components;
  using ProtoBuf.Grpc.Client;
  using System.Net.Http;
  using System.Threading.Tasks;
  using TimeWarp.Blazor.Features.Bases;
  using TimeWarp.Blazor.Features.Hellos;

  public partial class Index : BaseComponent
  {
    private const string RouteTemplate = "/";

    //[Inject]
    //public IHelloService HelloService { get; set; }


    public static string GetRoute() => RouteTemplate;

    public HelloResponse HelloResponse { get; set; }
    public HelloRequest HelloRequest { get; set; } = new HelloRequest { Name = "Yo" };

    //async Task Submit() =>
    //  HelloResponse = await HelloService.SayHelloAsync(HelloRequest);

    async Task Submit()
    {
      var handler = new Grpc.Net.Client.Web.GrpcWebHandler(Grpc.Net.Client.Web.GrpcWebMode.GrpcWeb, new HttpClientHandler());

      using var channel = Grpc.Net.Client.GrpcChannel.ForAddress
        (
          "https://localhost:5001/",
          new Grpc.Net.Client.GrpcChannelOptions() { HttpClient = new HttpClient(handler) }
        );

      IHelloService helloService = channel.CreateGrpcService<IHelloService>();
      HelloResponse = await helloService.SayHelloAsync(HelloRequest);
    }
  }
}
