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

  public static Task Should_Read_The_Real_OperationAttribute()
  {
    const string source = """
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

    EndpointEmitModel model = ModelFrom(source, "OperationMetadataReal");
    model.OperationName.ShouldBe("roles.get");
    model.OperationDescription.ShouldBe("Get one role.");
    return Task.CompletedTask;
  }

  public static Task Should_Drop_A_Whitespace_Operation_Name()
  {
    const string source = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation(" ", Description = "Get one role.")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles/{RoleId}", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }

              public sealed class Response { }
          }
      }
      """;

    EndpointEmitModel model = ModelFrom(source, "OperationMetadataBlankName");
    model.OperationName.ShouldBeNull();
    model.OperationDescription.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Drop_A_Whitespace_Operation_Description()
  {
    const string source = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("roles.get", Description = "  ")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles/{RoleId}", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }

              public sealed class Response { }
          }
      }
      """;

    EndpointEmitModel model = ModelFrom(source, "OperationMetadataBlankDescription");
    model.OperationName.ShouldBe("roles.get");
    model.OperationDescription.ShouldBeNull();
    return Task.CompletedTask;
  }

  private static EndpointEmitModel ModelFrom(string requestSource, string assemblyName)
  {
    const string stubs = """
      namespace TimeWarp.Foundation.Features
      {
          public enum HttpVerb { Get }
          public sealed class ApiRouteAttribute : System.Attribute
          {
              public ApiRouteAttribute(string route, HttpVerb verb) { }
          }
      }
      """;

    string runtime = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
      .Split(Path.PathSeparator)
      .First(static path => Path.GetFileName(path).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase));
    CSharpCompilation compilation = CSharpCompilation.Create(
      assemblyName,
      [CSharpSyntaxTree.ParseText(stubs), CSharpSyntaxTree.ParseText(requestSource)],
      [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(runtime),
        MetadataReference.CreateFromFile(typeof(TimeWarp.Contracts.OperationAttribute).Assembly.Location)
      ],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    ImmutableArray<Diagnostic> errors =
    [
      .. compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    ];
    errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors.Select(static diagnostic => diagnostic.ToString())));

    INamedTypeSymbol outer = compilation.GetTypeByMetadataName("Test.Features.Admin.Roles.GetRole")!;
    outer.GetTypeMembers("Query").Single().GetAttributes()
      .Any(static attribute =>
        attribute.AttributeClass is { TypeKind: not TypeKind.Error } symbol
        && symbol.ToDisplayString() == "TimeWarp.Contracts.OperationAttribute")
      .ShouldBeTrue();
    return EndpointEmitModel.FromSymbol(outer);
  }
}
