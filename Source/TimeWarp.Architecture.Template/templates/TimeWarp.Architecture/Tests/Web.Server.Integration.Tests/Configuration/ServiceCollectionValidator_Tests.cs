namespace ServiceCollectionValidator_;

using static TimeWarp.Architecture.Configuration.Constants;

public class Validate_Should
{
  private ServiceCollectionOptionsValidator ServiceCollectionValidator;

  public void Be_Valid()
  {
    var serviceCollection = new ServiceCollectionOptions
    {
      {WebServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
      {GrpcServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
      {ApiServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
    };

    ValidationResult validationResult = ServiceCollectionValidator.TestValidate(serviceCollection);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_a_Service_is_missing()
  {
    var serviceCollection = new ServiceCollectionOptions
    {
      {"wrong_Id", new ServiceCollectionOptions.Service { Host = "", Protocol="", Port=0} },
    };

    TestValidationResult<ServiceCollectionOptions> result =
      ServiceCollectionValidator.TestValidate(serviceCollection);

    result.ShouldHaveValidationErrorFor(aServiceCollection => aServiceCollection)
      .WithErrorMessage($"The {GrpcServiceName} service must be configured.");

    //result.ShouldHaveValidationErrorFor(aServiceCollection => aServiceCollection)
    //  .WithErrorMessage($"The {Constants.ApiServiceName} service must be configured.");

    //result.ShouldHaveValidationErrorFor(aServiceCollection => aServiceCollection)
    //  .WithErrorMessage($"The {Constants.WebServiceName} service must be configured.");
  }

  public void Have_error_when_a_Service_is_invalid()
  {
    var serviceCollection = new ServiceCollectionOptions
    {
      {GrpcServiceName, new ServiceCollectionOptions.Service { Host = "", Protocol="", Port=0} },
      {ApiServiceName, new ServiceCollectionOptions.Service { Host = "", Protocol="", Port=0} },
      {WebServiceName, new ServiceCollectionOptions.Service { Host = "", Protocol="", Port=-10} },
    };

    TestValidationResult<ServiceCollectionOptions> result =
      ServiceCollectionValidator.TestValidate(serviceCollection);

    ITestValidationContinuation x = result.ShouldHaveAnyValidationError();
    result.Errors.Count.Should().Be(3);
    result.Errors.Where(e => e.PropertyName == nameof(ServiceCollectionOptions.Service.Host)).Count().Should().Be(1);
    result.Errors.Where(e => e.PropertyName == nameof(ServiceCollectionOptions.Service.Port)).Count().Should().Be(1);
    result.Errors.Where(e => e.PropertyName == nameof(ServiceCollectionOptions.Service.Protocol)).Count().Should().Be(1);
  }

  public void appsettings_can_bind_ServiceCollection()
  {
    string json = @"
{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Debug"",
      ""System"": ""Information"",
      ""Microsoft"": ""Information""
    }
  },
  ""service"": {
    ""web-server"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""7001""
    },
    ""api-server"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""7001""
    },
    ""grpc-server"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""7001""
    }
  }
}
";

    var stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(json));
    var configurationBuilder = new ConfigurationBuilder();
    configurationBuilder
      .AddJsonStream(stream)
      .AddEnvironmentVariables();

    IConfigurationRoot config = configurationBuilder.Build();

    ServiceCollectionOptions serviceCollection = new();
    config.GetSection("service").Bind(serviceCollection);

    serviceCollection.Count.Should().Be(3);
    serviceCollection["web-server"].Host.Should().Be("localhost");
    serviceCollection["web-server"].Protocol.Should().Be("https");
    serviceCollection["web-server"].Port.Should().Be(7001);

    TestValidationResult<ServiceCollectionOptions> result =
      ServiceCollectionValidator.TestValidate(serviceCollection);

    result.IsValid.Should().BeTrue();
  }

  public void Setup() => ServiceCollectionValidator = new ServiceCollectionOptionsValidator();
}
