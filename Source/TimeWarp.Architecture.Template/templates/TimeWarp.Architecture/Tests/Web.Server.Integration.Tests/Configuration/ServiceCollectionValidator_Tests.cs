namespace ServiceCollectionValidator_;

using FluentAssertions;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;
using System.IO;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Configuration;

public class Validate_Should
{
  private ServiceCollectionOptionsValidator ServiceCollectionValidator;

  public void Be_Valid()
  {
    var serviceCollection = new ServiceCollectionOptions
    {
      {Constants.WebServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
      {Constants.GrpcServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
      {Constants.ApiServiceName, new ServiceCollectionOptions.Service { Host = "myhost", Protocol="https", Port=7001} },
    };

    ValidationResult validationResult = ServiceCollectionValidator.TestValidate(serviceCollection);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_a_Service_is_missing_or_invalid()
  {
    var serviceCollection = new ServiceCollectionOptions
    {
      {"wrong_Id", new ServiceCollectionOptions.Service { Host = "", Protocol="", Port=0} },
    };

    TestValidationResult<ServiceCollectionOptions> result =
      ServiceCollectionValidator.TestValidate(serviceCollection);

    result.ShouldHaveValidationErrorFor(aServiceCollection => aServiceCollection)
      .WithErrorMessage($"The {Constants.GrpcServiceName} service must be configured.")
      .WithErrorMessage($"The {Constants.ApiServiceName} service must be configured.")
      .WithErrorMessage($"The {Constants.WebServiceName} service must be configured.");

    result.ShouldHaveAnyValidationError()
      .WithErrorMessage("Service[0].Protocol must be assigned.")
      .WithErrorMessage("Service[0].Host must be assigned.")
      .WithErrorMessage("Service[0].Port must be greater than '0' but was '0'.");
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
