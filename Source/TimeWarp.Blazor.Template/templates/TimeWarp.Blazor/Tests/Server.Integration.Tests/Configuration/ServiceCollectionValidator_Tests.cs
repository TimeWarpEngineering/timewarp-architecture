namespace ServiceCollectionValidator_;

using FluentAssertions;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.WeatherForecasts;

public class Validate_Should
{
  private ServiceCollectionValidator ServiceCollectionValidator;

  public void Be_Valid()
  {
    var serviceCollection = new ServiceCollection
    {
      {Constants.WebServiceName, new ServiceCollection.Service { Host = "myhost", Protocol="https", Port=5001} },
      {Constants.GrpcServiceName, new ServiceCollection.Service { Host = "myhost", Protocol="https", Port=5001} },
      {Constants.ApiServiceName, new ServiceCollection.Service { Host = "myhost", Protocol="https", Port=5001} },
    };

    ValidationResult validationResult = ServiceCollectionValidator.TestValidate(serviceCollection);

    validationResult.IsValid.Should().BeTrue();
  }

  public void Have_error_when_a_Service_is_missing_or_invalid()
  {
    var serviceCollection = new ServiceCollection
    {
      {"wrong_Id", new ServiceCollection.Service { Host = "", Protocol="", Port=0} },
    };

    TestValidationResult<ServiceCollection> result =
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
    ""timewarp-blazor-server"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""5001""
    },
    ""timewarp-blazor-api"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""5001""
    },
    ""timewarp-blazor-grpcserver"": {
      ""protocol"": ""https"",
      ""host"": ""localhost"",
      ""port"": ""5001""
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

    ServiceCollection serviceCollection = new();
    config.GetSection("service").Bind(serviceCollection);

    serviceCollection.Count.Should().Be(3);
    serviceCollection["timewarp-blazor-server"].Host.Should().Be("localhost");
    serviceCollection["timewarp-blazor-server"].Protocol.Should().Be("https");
    serviceCollection["timewarp-blazor-server"].Port.Should().Be(5001);

    TestValidationResult<ServiceCollection> result =
      ServiceCollectionValidator.TestValidate(serviceCollection);

    result.IsValid.Should().BeTrue();      
  }

    public void Setup() => ServiceCollectionValidator = new ServiceCollectionValidator();
  }
