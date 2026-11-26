#region Purpose
// The OpenAPI document publishes file uploads and downloads as binary bodies, never as JSON.
#endregion

#region Design
// Task 458-003. POST /api/feedback/attachments published each accepted image type with the JSON
// command, and GET /api/feedback/attachments/{attachmentId} published 200 as application/json,
// so a generated client POSTed JSON and JSON-parsed an image. The feedback operations are pinned
// to the exact shape: binary per media type, the required X-File-Name header with its limit, the
// 413 problem, and Content-Disposition on the download. The scan starts from endpoint metadata,
// not the document: every endpoint whose request or response type carries a Stream must publish
// no JSON body on that side, so the next file endpoint cannot regress. The host is the OpenAPI
// pipeline only: FastEndpoints discovers the web-server endpoints. No mediator graph.
#endregion

namespace OpenApiBinaryContent_;

using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OneOf;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Web.Server;
using TimeWarp.Foundation;
using TimeWarp.Foundation.Features;

[TestTag("OpenApi")]
public partial class Document_Given_
{
  private const string UploadPath = "/api/feedback/attachments";
  private const string DownloadPath = "/api/feedback/attachments/{attachmentId}";
  private const string ProblemJson = "application/problem+json";
  private const string ProblemSchema = "#/components/schemas/TimeWarpFoundationTypesSharedProblemDetails";
  private const string StreamComponent = "SystemIOStream";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Document_Given_>();

  public static async Task SetupOnce()
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
    });
    CommonServerModule.AddOpenApi(builder.Services, "v1", "Test API", agentBearer: null);
    App = builder.Build();
    App.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    CommonServerModule.UseScalarApiReference(App, "v1", "Test API");
    await App.StartAsync();
    Client = App.GetTestClient();
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

  public static async Task Upload_Should_Publish_A_Binary_Body_Per_Allowed_Type()
  {
    JsonElement requestBody = (await OperationAsync(UploadPath, "post")).GetProperty("requestBody");
    requestBody.GetProperty("required").GetBoolean().ShouldBeTrue();

    JsonElement content = requestBody.GetProperty("content");
    MediaTypes(content).ShouldBe(FeedbackAttachmentRules.AllowedContentTypes, ignoreOrder: true);
    foreach (JsonProperty mediaType in content.EnumerateObject())
    {
      IsBinary(mediaType.Value.GetProperty("schema")).ShouldBeTrue($"{mediaType.Name} is not string binary.");
    }
  }

  public static async Task Upload_Should_Require_The_File_Name_Header_With_Its_Limit()
  {
    JsonElement operation = await OperationAsync(UploadPath, "post");
    JsonElement header = operation.GetProperty("parameters").EnumerateArray()
      .Single(static parameter => parameter.GetProperty("in").GetString() == "header"
        && parameter.GetProperty("name").GetString() == FileUploadHeaders.FileName);

    header.GetProperty("required").GetBoolean().ShouldBeTrue();
    header.GetProperty("schema").GetProperty("type").GetString().ShouldBe("string");
    header.GetProperty("description").GetString()!
      .ShouldContain($"At most {FeedbackAttachmentRules.MaxFileNameLength} characters after decoding");
  }

  public static async Task Upload_Should_Publish_The_413_Problem()
  {
    JsonElement tooLarge = (await OperationAsync(UploadPath, "post")).GetProperty("responses").GetProperty("413");

    tooLarge.GetProperty("description").GetString()!.ShouldContain($"{FeedbackAttachmentRules.MaxBytes} bytes");
    tooLarge.GetProperty("content").GetProperty(ProblemJson).GetProperty("schema").GetProperty("$ref").GetString()
      .ShouldBe(ProblemSchema);
  }

  public static async Task Download_Should_Publish_Binary_Content_Per_Allowed_Type()
  {
    JsonElement success = (await OperationAsync(DownloadPath, "get")).GetProperty("responses").GetProperty("200");

    JsonElement content = success.GetProperty("content");
    MediaTypes(content).ShouldBe(FeedbackAttachmentRules.AllowedContentTypes, ignoreOrder: true);
    foreach (JsonProperty mediaType in content.EnumerateObject())
    {
      IsBinary(mediaType.Value.GetProperty("schema")).ShouldBeTrue($"{mediaType.Name} is not string binary.");
    }

    JsonElement disposition = success.GetProperty("headers").GetProperty("Content-Disposition");
    disposition.GetProperty("required").GetBoolean().ShouldBeTrue();
    disposition.GetProperty("schema").GetProperty("type").GetString().ShouldBe("string");
  }

  public static async Task No_Stream_Operation_Should_Publish_Json()
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
    List<string> checkedOperations = [];
    List<string> json = [];
    foreach (RouteEndpoint endpoint in ((IEndpointRouteBuilder)App!).DataSources
      .SelectMany(static source => source.Endpoints)
      .OfType<RouteEndpoint>())
    {
      EndpointDefinition? definition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
      bool streamRequest = definition is not null && HasStream(definition.ReqDtoType);
      bool streamResponse = definition is not null && SuccessArms(definition.ResDtoType).Any(HasStream);
      if (!streamRequest && !streamResponse)
      {
        continue;
      }

      string path = RouteConstraint().Replace("/" + endpoint.RoutePattern.RawText!.TrimStart('/'), "{$1}");
      foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
      {
        string label = $"{method} {path}";
        checkedOperations.Add(label);
        if (!TryGetOperation(paths, path, method.ToLowerInvariant(), out JsonElement operation))
        {
          json.Add($"{label} (not in the document)");
          continue;
        }

        if (streamRequest
          && operation.TryGetProperty("requestBody", out JsonElement requestBody)
          && HasJson(requestBody))
        {
          json.Add($"{label} request");
        }

        if (streamResponse)
        {
          foreach (JsonProperty response in operation.GetProperty("responses").EnumerateObject())
          {
            if (response.Name.StartsWith('2', StringComparison.Ordinal) && HasJson(response.Value))
            {
              json.Add($"{label} {response.Name}");
            }
          }
        }
      }
    }

    checkedOperations.Count.ShouldBeGreaterThan(1, "No endpoint whose request or response carries a Stream was found.");
    json.ShouldBeEmpty($"Stream bodies published as JSON: {string.Join(", ", json)}");
  }

  public static async Task No_Component_Should_Describe_A_Stream()
  {
    JsonElement schemas = (await DocumentRootAsync()).GetProperty("components").GetProperty("schemas");
    List<string> carriers = [];
    foreach (JsonProperty schema in schemas.EnumerateObject())
    {
      if (schema.Name == StreamComponent || schema.Value.GetRawText().Contains($"/{StreamComponent}\"", StringComparison.Ordinal))
      {
        carriers.Add(schema.Name);
      }
    }

    carriers.ShouldBeEmpty($"Components that describe a Stream as JSON: {string.Join(", ", carriers)}");
  }

  private static List<string> MediaTypes(JsonElement content) =>
    [.. content.EnumerateObject().Select(static mediaType => mediaType.Name)];

  private static bool IsBinary(JsonElement schema) =>
    schema.TryGetProperty("type", out JsonElement type)
    && type.GetString() == "string"
    && schema.TryGetProperty("format", out JsonElement format)
    && format.GetString() == "binary";

  private static bool HasJson(JsonElement body) =>
    body.TryGetProperty("content", out JsonElement content)
    && content.EnumerateObject().Any(static mediaType => IsJson(mediaType.Name));

  private static bool IsJson(string mediaType) =>
    mediaType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
    || mediaType.Contains("+json", StringComparison.OrdinalIgnoreCase)
    || string.Equals(mediaType, "*/*", StringComparison.Ordinal);

  private static bool HasStream(Type? type) =>
    type?.GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Any(static property => typeof(Stream).IsAssignableFrom(property.PropertyType)) == true;

  /// <summary>
  /// The response's arms other than SharedProblemDetails, or the type itself when it is not a OneOf.
  /// </summary>
  private static IEnumerable<Type> SuccessArms(Type? type)
  {
    for (Type? current = type; current is not null; current = current.BaseType)
    {
      if (current.IsGenericType
        && string.Equals(current.Namespace, typeof(IOneOf).Namespace, StringComparison.Ordinal))
      {
        return current.GetGenericArguments().Where(static arm => arm != typeof(SharedProblemDetails));
      }
    }

    return type is null ? [] : [type];
  }

  [GeneratedRegex(@"\{([^}:=?]+)[^}]*\}")]
  private static partial Regex RouteConstraint();

  private static async Task<JsonElement> OperationAsync(string path, string method)
  {
    JsonElement paths = (await DocumentRootAsync()).GetProperty("paths");
    if (TryGetOperation(paths, path, method, out JsonElement operation))
    {
      return operation;
    }

    string known = string.Join(", ", paths.EnumerateObject().Select(static item => item.Name));
    throw new InvalidOperationException($"Missing {method} {path}. Paths: {known}");
  }

  private static bool TryGetOperation(JsonElement paths, string path, string method, out JsonElement operation)
  {
    foreach (JsonProperty item in paths.EnumerateObject())
    {
      if (string.Equals(item.Name, path, StringComparison.OrdinalIgnoreCase)
        && item.Value.TryGetProperty(method, out operation))
      {
        operation = operation.Clone();
        return true;
      }
    }

    operation = default;
    return false;
  }

  private static async Task<JsonElement> DocumentRootAsync()
  {
    using HttpResponseMessage response = await Client!.GetAsync("/openapi/v1.json");
    string json = await response.Content.ReadAsStringAsync();
    // A transformer guard throws; the developer exception page carries its message here.
    response.StatusCode.ShouldBe(HttpStatusCode.OK, json[..Math.Min(json.Length, 2000)]);
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.Clone();
  }
}
