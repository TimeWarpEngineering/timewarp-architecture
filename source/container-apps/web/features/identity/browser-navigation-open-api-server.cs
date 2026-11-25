#region Purpose
// OpenAPI pieces the hand-written browser-navigation endpoints cannot declare through FastEndpoints.
#endregion

#region Design
// Task 458-007: sign-out, its antiforgery token, and the Entra challenge stay operations in the
// public document. They are not excluded: they are FastEndpoints, Scalar groups them under
// Identity, and an agent reading the document should see the real browser contract. Each endpoint
// declares its statuses with Produces and ClearDefaultProduces(204) in Configure. FastEndpoints
// has no request DTO for them, so the query parameters, the form field, and the Location header
// come from an endpoint-scoped AddOpenApiOperationTransformer that calls these helpers.
// The form field name is AntiforgeryOptions.FormFieldName, read from the host's options, so the
// document names the field the antiforgery check reads. Mode values are EntraTicketProcessor's
// constants, the ones the handler compares against.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

internal static class BrowserNavigationOpenApi
{
  internal const string FormContentType = "application/x-www-form-urlencoded";

  /// <summary>Adds a <c>Location</c> header to the <paramref name="status"/> response.</summary>
  internal static void AddLocation(OpenApiOperation operation, string status, string description, string? example)
  {
    ArgumentNullException.ThrowIfNull(operation);
    if (operation.Responses?.TryGetValue(status, out IOpenApiResponse? response) != true
      || response is not OpenApiResponse openApiResponse)
    {
      throw new InvalidOperationException
      (
        $"BrowserNavigationOpenApi: {operation.OperationId} has no {status} response to carry Location."
      );
    }

    openApiResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
    openApiResponse.Headers["Location"] = new OpenApiHeader
    {
      Description = description,
      Required = true,
      Schema = new OpenApiSchema { Type = JsonSchemaType.String },
      Example = example is null ? null : JsonValue.Create(example),
    };
  }

  /// <summary>Sets the request body to one required form field: the host's antiforgery field.</summary>
  internal static void SetAntiforgeryForm(OpenApiOperation operation, OpenApiOperationTransformerContext context)
  {
    ArgumentNullException.ThrowIfNull(operation);
    ArgumentNullException.ThrowIfNull(context);
    string fieldName = context.ApplicationServices
      .GetRequiredService<IOptions<AntiforgeryOptions>>().Value.FormFieldName;

    operation.RequestBody = new OpenApiRequestBody
    {
      Required = true,
      Description =
        "Browser form post. " + fieldName + " is the requestToken from GET "
        + SignOutBrowserSession.AntiforgeryTokenPath + ", sent with the antiforgery cookie that request set.",
      Content = new Dictionary<string, IOpenApiMediaType>
      {
        [FormContentType] = new OpenApiMediaType
        {
          Schema = new OpenApiSchema
          {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string> { fieldName },
            Properties = new Dictionary<string, IOpenApiSchema>
            {
              [fieldName] = new OpenApiSchema { Type = JsonSchemaType.String },
            },
          },
        },
      },
    };
  }

  /// <summary>Adds a string query parameter.</summary>
  internal static void AddQuery
  (
    OpenApiOperation operation,
    string name,
    string description,
    bool required,
    params string[] allowedValues
  )
  {
    ArgumentNullException.ThrowIfNull(operation);
    OpenApiSchema schema = new() { Type = JsonSchemaType.String };
    if (allowedValues.Length > 0)
    {
      schema.Enum = [.. allowedValues.Select(static value => (JsonNode)JsonValue.Create(value))];
    }

    operation.Parameters ??= [];
    operation.Parameters.Add
    (
      new OpenApiParameter
      {
        Name = name,
        In = ParameterLocation.Query,
        Required = required,
        Description = description,
        Schema = schema,
      }
    );
  }
}
