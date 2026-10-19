#region Purpose
// Endpoint emit metadata stores [Operation] name, description, and group from the nested request.
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
    model.OperationGroup.ShouldBeNull();
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
    model.OperationGroup.ShouldBeNull();
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
    model.OperationGroup.ShouldBeNull();
    model.Route.ShouldBe("api/Roles/{RoleId}");
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
    model.OperationGroup.ShouldBeNull();
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
    model.OperationGroup.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Should_Carry_Operation_Group_Without_Changing_The_Route()
  {
    const string source = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("get_symbol", Description = "Resolve one symbol.", Group = "symbols")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles/{RoleId}", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }

              public sealed class Response { }
          }
      }
      """;

    EndpointEmitModel model = ModelFrom(source, "OperationMetadataGroup");
    model.OperationName.ShouldBe("get_symbol");
    model.OperationDescription.ShouldBe("Resolve one symbol.");
    model.OperationGroup.ShouldBe("symbols");
    model.Route.ShouldBe("api/Roles/{RoleId}");
    return Task.CompletedTask;
  }

  public static Task Should_Keep_Dotted_And_Spaced_Operation_Groups()
  {
    const string dotted = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("rename_symbol", Group = "edit.symbol")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }
          }
      }
      """;

    const string spaced = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("rename_parameter", Group = "edit symbol")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }
          }
      }
      """;

    EndpointEmitModel dottedModel = ModelFrom(dotted, "OperationMetadataDottedGroup");
    EndpointEmitModel spacedModel = ModelFrom(spaced, "OperationMetadataSpacedGroup");
    dottedModel.OperationName.ShouldBe("rename_symbol");
    dottedModel.OperationGroup.ShouldBe("edit.symbol");
    dottedModel.Route.ShouldBe("api/Roles");
    spacedModel.OperationName.ShouldBe("rename_parameter");
    spacedModel.OperationGroup.ShouldBe("edit symbol");
    spacedModel.Route.ShouldBe("api/Roles");
    return Task.CompletedTask;
  }

  public static Task Should_Drop_A_Whitespace_Operation_Group()
  {
    const string source = """
      namespace Test.Features.Admin.Roles
      {
          public static class GetRole
          {
              [TimeWarp.Contracts.Operation("roles.get", Description = "Get one role.", Group = "  ")]
              [TimeWarp.Foundation.Features.ApiRoute("api/Roles/{RoleId}", TimeWarp.Foundation.Features.HttpVerb.Get)]
              public sealed class Query { }

              public sealed class Response { }
          }
      }
      """;

    EndpointEmitModel model = ModelFrom(source, "OperationMetadataBlankGroup");
    model.OperationName.ShouldBe("roles.get");
    model.OperationDescription.ShouldBe("Get one role.");
    model.OperationGroup.ShouldBeNull();
    model.Route.ShouldBe("api/Roles/{RoleId}");
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
