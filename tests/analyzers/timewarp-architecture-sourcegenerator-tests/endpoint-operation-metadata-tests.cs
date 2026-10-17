#region Purpose
// Endpoint emit metadata stores [Operation] name and description from the nested request.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

using TimeWarp.Architecture.Analyzers.Models;

public class EndpointOperationMetadata_Tests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<EndpointOperationMetadata_Tests>();

  public static Task Should_Carry_Operation_Name_And_Description()
  {
    const string source = """
      namespace TimeWarp.Contracts
      {
          [System.AttributeUsage(System.AttributeTargets.Class)]
          public sealed class OperationAttribute : System.Attribute
          {
              public OperationAttribute(string name) { Name = name; }
              public string Name { get; }
              public string? Description { get; set; }
          }
      }

      namespace TimeWarp.Foundation.Features
      {
          public enum HttpVerb { Get }
          public sealed class ApiRouteAttribute : System.Attribute
          {
              public ApiRouteAttribute(string route, HttpVerb verb) { }
          }
      }

      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("roles.get", Description = "Get one role.")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles/{RoleId}", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }

              public sealed class Response { }
          }
      }
      """;

    CSharpCompilation compilation = CSharpCompilation.Create(
      "OperationMetadata",
      [CSharpSyntaxTree.ParseText(source)],
      [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    INamedTypeSymbol? outer = compilation.GetTypeByMetadataName("Test.Features.Admin.Roles.GetRole");
    outer.ShouldNotBeNull();

    EndpointEmitModel model = EndpointEmitModel.FromSymbol(outer);
    model.OperationName.ShouldBe("roles.get");
    model.OperationDescription.ShouldBe("Get one role.");
    model.RequestTypeName.ShouldBe("Query");
    return Task.CompletedTask;
  }

  public static Task Should_Leave_Operation_Empty_When_The_Request_Has_None()
  {
    const string source = """
      namespace TimeWarp.Foundation.Features
      {
          public enum HttpVerb { Get }
          public sealed class ApiRouteAttribute : System.Attribute
          {
              public ApiRouteAttribute(string route, HttpVerb verb) { }
          }
      }

      namespace Test.Features.WeatherForecast
      {
          public static class GetWeatherForecasts
          {
              [TimeWarp.Foundation.Features.ApiRoute("api/weatherForecasts", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }
          }
      }
      """;

    CSharpCompilation compilation = CSharpCompilation.Create(
      "OperationMetadataAbsent",
      [CSharpSyntaxTree.ParseText(source)],
      [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    INamedTypeSymbol outer = compilation.GetTypeByMetadataName("Test.Features.WeatherForecast.GetWeatherForecasts")!;
    EndpointEmitModel model = EndpointEmitModel.FromSymbol(outer);
    model.OperationName.ShouldBeNull();
    model.OperationDescription.ShouldBeNull();
    return Task.CompletedTask;
  }
}
