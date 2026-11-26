#region Purpose
// Request members a validator rejects when omitted are required in the OpenAPI document, and the document check catches a lost one.
#endregion

#region Design
// Task 458-006. Schema export does not see FluentValidation, so commands published every member
// as optional. OpenApiValidatorRequired reads the validators FluentValidationBehavior runs. The
// first host registers web-server's endpoints and its contract validators, as program.cs does,
// so the document check runs over every operation; the document must still build. CreateRole
// gets its rules through RuleFor(x => x).SetValidator, SubmitFeedback through Include. A rule
// under When, a NotNull member initialized to [], and a member with no rule stay optional.
// The second host strips required from every component after the shared transformers, as a
// later host transformer could, and the document check must fail the request naming the member.
// Both hosts are the OpenAPI pipeline only. No mediator graph.
#endregion

namespace OpenApiRequiredRequestMembers_;

using System.Net;
using System.Text.Json;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentLinks;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;

[TestTag("OpenApi")]
public class Document_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Document_Given_>();

  public static async Task SetupOnce()
  {
    (App, Client) = await RequiredMembersHost.StartAsync(endpointName: null, configureOpenApi: null);
  }

  public static async Task CleanUpOnce()
  {
    Client?.Dispose();
    Client = null;
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static async Task Document_Should_Build_With_Every_Web_Server_Validator()
  {
    using HttpResponseMessage response = await Client!.GetAsync(RequiredMembersHost.DocumentPath);
    response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
  }

  public static async Task CreateRole_Should_Require_The_Members_Its_Shared_Validator_Rejects()
  {
    JsonElement schema = await RequestSchemaAsync("/" + CreateRole.Command.RouteTemplate, "post");

    Required(schema).ShouldBe(["description", "name"], ignoreOrder: true);
  }

  public static async Task SetXaiApiKey_Should_Require_ApiKey()
  {
    JsonElement schema = await RequestSchemaAsync("/" + SetXaiApiKey.Command.RouteTemplate, "put");

    Required(schema).ShouldBe(["apiKey"]);
  }

  public static async Task SubmitFeedback_Should_Require_Only_Members_Whose_Omission_Fails()
  {
    JsonElement schema = await RequestSchemaAsync("/api/feedback", "post");
    string[] required = Required(schema);

    required.ShouldContain("title");
    required.ShouldContain("body");
    required.ShouldNotContain("attachmentIds", "NotNull on a member initialized to [] passes when omitted.");
    required.ShouldNotContain("pagePath", "The PagePath rule runs only When it is present.");
    required.ShouldNotContain("emailCopy", "EmailCopy has no rule.");
  }

  public static async Task RequestAgentHumanLink_Should_Require_HumanPrincipalId_Without_An_Attribute()
  {
    typeof(RequestAgentHumanLink.Command).GetProperty(nameof(RequestAgentHumanLink.Command.HumanPrincipalId))!
      .GetCustomAttributes(inherit: true)
      .ShouldNotContain(static attribute => attribute is System.ComponentModel.DataAnnotations.RequiredAttribute);
    JsonElement schema = await RequestSchemaAsync("/" + RequestAgentHumanLink.Command.RouteTemplate, "post");

    Required(schema).ShouldContain("humanPrincipalId");
  }

  private static async Task<JsonElement> RequestSchemaAsync(string path, string method)
  {
    using HttpResponseMessage response = await Client!.GetAsync(RequiredMembersHost.DocumentPath);
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    JsonElement root = document.RootElement;
    JsonElement schema = root
      .GetProperty("paths")
      .GetProperty(path)
      .GetProperty(method)
      .GetProperty("requestBody")
      .GetProperty("content")
      .GetProperty("application/json")
      .GetProperty("schema");

    if (schema.TryGetProperty("$ref", out JsonElement reference))
    {
      string name = reference.GetString()!.Split('/')[^1];
      schema = root.GetProperty("components").GetProperty("schemas").GetProperty(name);
    }

    return schema.Clone();
  }

  private static string[] Required(JsonElement schema) =>
    schema.TryGetProperty("required", out JsonElement required)
      ? [.. required.EnumerateArray().Select(static item => item.GetString()!)]
      : [];
}

[TestTag("OpenApi")]
public class RequiredStripped_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RequiredStripped_Given_>();

  public static async Task SetupOnce()
  {
    (App, Client) = await RequiredMembersHost.StartAsync
    (
      endpointName: "RequestAgentHumanLinkEndpoint",
      configureOpenApi: static openApiOptions => openApiOptions.AddDocumentTransformer
      (
        static (document, context, cancellationToken) =>
        {
          _ = context;
          _ = cancellationToken;
          foreach (IOpenApiSchema component in document.Components?.Schemas?.Values ?? [])
          {
            if (component is OpenApiSchema concrete)
            {
              concrete.Required = null;
            }
          }

          return Task.CompletedTask;
        }
      )
    );
  }

  public static async Task CleanUpOnce()
  {
    Client?.Dispose();
    Client = null;
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static async Task Document_Check_Should_Fail_Naming_The_Member()
  {
    using HttpResponseMessage response = await Client!.GetAsync(RequiredMembersHost.DocumentPath);
    string body = await response.Content.ReadAsStringAsync();

    response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    body.ShouldContain("OpenApiValidatorRequired");
    body.ShouldContain("humanPrincipalId");
  }
}

internal static class RequiredMembersHost
{
  internal const string DocumentPath = "/openapi/v1.json";

  /// <summary>
  /// Web-server endpoints (or the one named) with web-server's contract validators registered.
  /// </summary>
  internal static async Task<(WebApplication App, HttpClient Client)> StartAsync
  (
    string? endpointName,
    Action<Microsoft.AspNetCore.OpenApi.OpenApiOptions>? configureOpenApi
  )
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder
    (
      new WebApplicationOptions
      {
        EnvironmentName = Environments.Development,
      }
    );
    builder.WebHost.UseTestServer();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(Program).Assembly];
      if (endpointName is not null)
      {
        options.Filter = type => string.Equals(type.Name, endpointName, StringComparison.Ordinal);
      }
    });
    builder.Services.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Server.IAssemblyMarker>();
    builder.Services.AddValidatorsFromAssemblyContaining<TimeWarp.Architecture.Web.Contracts.IAssemblyMarker>();
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API", agentBearer: null, configureOpenApi: configureOpenApi);
    WebApplication app = builder.Build();
    app.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    CommonServerModule.UseScalarApiReference(app, "v1", "Test API");
    await app.StartAsync();
    return (app, app.GetTestClient());
  }
}
