namespace TimeWarp.Architecture.Features.Superheros;

public class SuperheroModule
{
  public static void ConfigureServices(IServiceCollection aServiceCollection, IConfiguration aConfiguration)
  {

    aServiceCollection.AddSingleton
    (
      serviceProvider =>
      {
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

        Uri grpcUrl = GetServiceUri(configuration, Constants.GrpcServiceName);

        Console.WriteLine($"grpcUrl:{grpcUrl}");

        if (grpcUrl is null)
        {
          throw new Exception($"No {Constants.GrpcServiceName} address found in configuration");
        }


        // Create a channel with a GrpcWebHandler that is addressed to the backend server.
        //
        // GrpcWebText is used because server streaming requires it. If server streaming is not used in your app
        // then GrpcWeb is recommended because it produces smaller messages.
        var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());

        return GrpcChannel.ForAddress
        (
          grpcUrl,
          new GrpcChannelOptions
          {
            HttpHandler = httpHandler,
            //CompressionProviders = ...,
            //Credentials = ...,
            //DisposeHttpClient = ...,
            //HttpClient = ...,
            //LoggerFactory = ...,
            //MaxReceiveMessageSize = ...,
            //MaxSendMessageSize = ...,
            //ThrowOperationCanceledOnCancellation = ...,
          }
        );
      }
    );

    aServiceCollection.AddSingleton<ISuperheroService>
    (
      aServiceProvider =>
      {
        GrpcChannel grpcChannel = aServiceProvider.GetRequiredService<GrpcChannel>();
        return grpcChannel.CreateGrpcService<ISuperheroService>();
      }
    );

  }

  private static Uri GetServiceUri(IConfiguration aConfiguration, string aServiceName)
  {
    ServiceCollectionOptions serviceCollectionOptions =
      aConfiguration.GetSection(nameof(ServiceCollectionOptions)).Get<ServiceCollectionOptions>() ??
      throw new InvalidOperationException();

    ServiceCollectionOptions.Service service = serviceCollectionOptions[Constants.GrpcServiceName];

    Console.WriteLine($"service.Host:{service.Host}");

    var uriBuilder = new UriBuilder
    {
      Scheme = service.Protocol,
      Host = service.Host,
      Port = service.Port
    };

    Uri serviceUri = aConfiguration.GetServiceHttpsUri(aServiceName) ?? uriBuilder.Uri;
    Console.WriteLine($"serviceUri:{serviceUri}");
    // https://github.com/dotnet/aspire/issues/2549

    return serviceUri;
  }
}


