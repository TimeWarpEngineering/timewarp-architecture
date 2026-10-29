#region Purpose
// Tells OpenAPI readers which public web routes exist outside the generated FastEndpoints operations.
#endregion

#region Design
// ExcludeNonFastEndpoints stays on, so /api/debug-config and other minimal APIs stay out of the
// document. Web-server opts in. Api-server does not: it does not serve these routes.
// GET /api/health is MapHealthChecks. The body is the status name as text/plain. The Postgres
// check, when registered, opens a connection, so this is not the liveness probe.
// GET and POST /api are TipDiscoveryAlias rewrites of /api/tip. The alias is its own path so a
// missing entry is not read as a missing route. The payment contract stays on /api/tip.
// /.well-known discovery documents are excluded. info.description is what Scalar renders, and
// x-timewarp-excluded-routes is the same list for a reader that does not parse markdown.
// Both point at /llms.txt.
#endregion

namespace TimeWarp.Foundation;

using System.Net.Http;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

/// <summary>
/// Adds the public web routes that are not generated FastEndpoints operations.
/// </summary>
internal static class OpenApiDiscoveryCoverage
{
  internal const string ExtensionName = "x-timewarp-excluded-routes";

  internal const string HealthPath = TimeWarp.Foundation.Configuration.IngressPaths.WebServerHealthPath;

  internal const string TipAliasPath = TimeWarp.Foundation.Configuration.IngressPaths.TipDiscoveryAliasPath;

  internal const string TipPath = "/api/tip";

  internal const string OutsideHeading = "## Routes outside this document";

  internal const string AliasSentence =
    "GET and POST /api and /api/ rewrite to /api/tip before routing.";

  private readonly record struct ExcludedRoute(string Path, string Reason);

  private static readonly ExcludedRoute[] ExcludedRoutes =
  [
    new("/.well-known/mcp/server-card.json", "Static MCP server card. capabilities.tools is false."),
    new("/.well-known/mcp.json", "Alias of /.well-known/mcp/server-card.json."),
    new("/.well-known/agent-skills/index.json", "Static Agent Skills index."),
    new("/.well-known/agent-card.json", "Unsigned A2A agent card. No JSON-RPC task endpoint."),
    new("/.well-known/agent.json", "Alias of /.well-known/agent-card.json."),
    new("/.well-known/webmcp.json", "Generated WebMCP document for the browser, not this REST API."),
  ];

  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    openApiOptions.AddDocumentTransformer(AddCoverageAsync);
  }

  private static Task AddCoverageAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = context;
    _ = cancellationToken;
    if (document.Extensions?.ContainsKey(ExtensionName) == true)
    {
      return Task.CompletedTask;
    }

    document.Paths ??= new OpenApiPaths();
    AddHealth(document);
    AddTipAlias(document);
    NoteCanonicalTip(document);
    NoteExclusions(document);
    document.AddExtension(ExtensionName, new JsonNodeExtension(ExcludedArray()));
    return Task.CompletedTask;
  }

  private static void AddHealth(OpenApiDocument document)
  {
    if (document.Paths.ContainsKey(HealthPath))
    {
      return;
    }

    OpenApiPathItem pathItem = new();
    pathItem.AddOperation
    (
      HttpMethod.Get,
      AnonymousOperation
      (
        document,
        "getApiHealth",
        "Web server health",
        "Anonymous ASP.NET health check. The body is text/plain and is one status name: "
        + "Healthy, Degraded, or Unhealthy. 200 is Healthy or Degraded. 503 is Unhealthy. "
        + "When the PostgresDbContext check is registered, this route opens a database connection. "
        + "Do not use it as an uptime probe. Liveness is GET /web-server/alive.",
        "Health",
        ("200", PlainText("Healthy or Degraded. The body is that status name.")),
        ("503", PlainText("Unhealthy. The body is Unhealthy."))
      )
    );
    document.Paths.Add(HealthPath, pathItem);
  }

  private static void AddTipAlias(OpenApiDocument document)
  {
    if (document.Paths.ContainsKey(TipAliasPath))
    {
      return;
    }

    const string description =
      AliasSentence + " "
      + "The contract, including 402 PAYMENT-REQUIRED and 503 when payment is disabled or misconfigured, "
      + "is the /api/tip operation in this document. The challenge resource stays /api/tip. Anonymous.";

    OpenApiPathItem pathItem = new();
    pathItem.AddOperation
    (
      HttpMethod.Get,
      AnonymousOperation
      (
        document,
        "getApiTipAlias",
        "Discovery alias of GET /api/tip",
        description,
        "Tip",
        ("200", new OpenApiResponse { Description = "Same success response as GET /api/tip." }),
        ("402", new OpenApiResponse { Description = "Same 402 PAYMENT-REQUIRED challenge as GET /api/tip." }),
        ("503", new OpenApiResponse { Description = "Same 503 as GET /api/tip when payment is disabled or misconfigured." })
      )
    );
    pathItem.AddOperation
    (
      HttpMethod.Post,
      AnonymousOperation
      (
        document,
        "postApiTipAlias",
        "Discovery alias of POST /api/tip",
        description,
        "Tip",
        ("200", new OpenApiResponse { Description = "Same success response as POST /api/tip." }),
        ("402", new OpenApiResponse { Description = "Same 402 PAYMENT-REQUIRED challenge as POST /api/tip." }),
        ("503", new OpenApiResponse { Description = "Same 503 as POST /api/tip when payment is disabled or misconfigured." })
      )
    );
    document.Paths.Add(TipAliasPath, pathItem);
  }

  private static void NoteCanonicalTip(OpenApiDocument document)
  {
    if (!document.Paths.TryGetValue(TipPath, out IOpenApiPathItem? pathItem) || pathItem.Operations is null)
    {
      return;
    }

    foreach (OpenApiOperation operation in pathItem.Operations.Values)
    {
      if (operation.Description?.Contains(AliasSentence, StringComparison.Ordinal) == true)
      {
        continue;
      }

      operation.Description = string.IsNullOrWhiteSpace(operation.Description)
        ? AliasSentence
        : operation.Description.TrimEnd() + " " + AliasSentence;
    }
  }

  private static void NoteExclusions(OpenApiDocument document)
  {
    if (document.Info.Description?.Contains(OutsideHeading, StringComparison.Ordinal) == true)
    {
      return;
    }

    string note = Description();
    document.Info.Description = string.IsNullOrWhiteSpace(document.Info.Description)
      ? note
      : document.Info.Description.TrimEnd() + "\n\n" + note;
  }

  private static string Description()
  {
    string lines =
      OutsideHeading + "\n\n"
      + "This document includes the web-server FastEndpoints operations, `GET /api/health`, "
      + "and `GET` and `POST /api` (a rewrite alias of `/api/tip`, including `/api/`).\n\n"
      + "The discovery documents below are not operations. A missing path is this exclusion, "
      + "not a missing route. Scalar ([/scalar/v1](/scalar/v1)) shows this note. "
      + "Index: [/llms.txt](/llms.txt). The same list is `" + ExtensionName + "`.\n";

    foreach (ExcludedRoute route in ExcludedRoutes)
    {
      lines += "\n- `GET " + route.Path + "` — " + route.Reason + " [" + route.Path + "](" + route.Path + ")";
    }

    return lines;
  }

  private static JsonArray ExcludedArray()
  {
    JsonArray array = [];
    foreach (ExcludedRoute route in ExcludedRoutes)
    {
      array.Add
      (
        new JsonObject
        {
          ["path"] = route.Path,
          ["method"] = "GET",
          ["reason"] = route.Reason,
          ["see"] = "/llms.txt",
        }
      );
    }

    return array;
  }

  private static OpenApiOperation AnonymousOperation
  (
    OpenApiDocument document,
    string operationId,
    string summary,
    string description,
    string tag,
    params (string Status, OpenApiResponse Response)[] responses
  )
  {
    OpenApiResponses openApiResponses = new();
    foreach ((string status, OpenApiResponse response) in responses)
    {
      openApiResponses.Add(status, response);
    }

    return new OpenApiOperation
    {
      OperationId = operationId,
      Summary = summary,
      Description = description,
      Security = [],
      Tags = new HashSet<OpenApiTagReference>
      {
        new(tag, document, null),
      },
      Responses = openApiResponses,
    };
  }

  private static OpenApiResponse PlainText(string description)
  {
    return new OpenApiResponse
    {
      Description = description,
      Content = new Dictionary<string, OpenApiMediaType>
      {
        ["text/plain"] = new OpenApiMediaType
        {
          Schema = new OpenApiSchema
          {
            Type = JsonSchemaType.String,
          },
        },
      },
    };
  }
}
