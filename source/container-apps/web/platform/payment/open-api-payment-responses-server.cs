#region Purpose
// Documents HTTP 402 and 503 on the tip jar and metered capability, and states that free routes never return 402.
#endregion

#region Design
// FastEndpoints infers the success body, so the document otherwise shows only 200.
// 402 and 503 are SharedProblemDetails plus, on 402 only, the PAYMENT-REQUIRED header. Each body
// schema is allOf: a $ref to the one SharedProblemDetails component (OpenApiProblemDetails), then
// the extension members the payment gate adds (payment, reason or error and ok, tip on the tip jar).
// That header is base64 x402 v2 JSON (x402Version, accepts, resource), so the header schema
// carries contentEncoding base64 and the decoded object as contentSchema.
// Routes come from the contract RouteTemplate constants. The bare /api alias is a rewrite
// onto the tip jar, not its own operation. Free and discovery operations are left without
// 402 or 503. Challenge resource.url is TipOptions.Resource or MeteredCapabilityOptions.Resource,
// read from the host services, not a second copy of the route. The document description says
// so, so a 402 on a paid operation is not read
// as a response of this document, health, or identity ceremonies.
// Metered calls still require bearer scope demo:invoke. That scope authorizes the attempt;
// payment is separate, and a 200 debits the ledger.
#endregion

namespace TimeWarp.Architecture.Payment;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using TimeWarp.Architecture.Features.MeteredCapability;
using TimeWarp.Architecture.Features.Tip;
using TimeWarp.Identity;
using TimeWarp.X402;

/// <summary>
/// Adds the paid-route payment responses to the web OpenAPI document.
/// </summary>
internal static class OpenApiPaymentResponses
{
  private static readonly string TipRoute = Normalize(SubmitTip.Query.RouteTemplate);
  private static readonly string MeteredRoute = Normalize(InvokeMeteredCapability.Query.RouteTemplate);

  private static readonly string FreeRoutesNever402 =
    "Free and discovery operations never return HTTP 402. "
    + $"HTTP 402 and HTTP 503 are documented only on GET and POST /{TipRoute} and on GET /{MeteredRoute}. "
    + "This document, health, identity ceremonies, and other free routes are not payment challenges. "
    + "The bare /api path is a discovery alias of the tip jar, not a separate operation, and follows the tip jar. "
    + "Disabled or misconfigured payment returns 503 on those paid routes and does not return 402.";

  internal static void Register(OpenApiOptions openApiOptions)
  {
    ArgumentNullException.ThrowIfNull(openApiOptions);
    string postRoute = Normalize(SubmitTipPost.Command.RouteTemplate);
    if (!string.Equals(TipRoute, postRoute, StringComparison.Ordinal))
    {
      throw new InvalidOperationException("Tip GET and POST must share one route so the OpenAPI payment responses stay on both verbs.");
    }

    openApiOptions.AddDocumentTransformer(DescribeFreeRoutesAsync);
    openApiOptions.AddOperationTransformer(DocumentPaidOperationAsync);
  }

  private static Task DescribeFreeRoutesAsync
  (
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    _ = context;
    _ = cancellationToken;
    document.Info ??= new OpenApiInfo();
    document.Info.Description = AppendNote(document.Info.Description, FreeRoutesNever402);
    return Task.CompletedTask;
  }

  private static async Task DocumentPaidOperationAsync
  (
    OpenApiOperation operation,
    OpenApiOperationTransformerContext context,
    CancellationToken cancellationToken
  )
  {
    string path = Normalize(context.Description.RelativePath);
    string? method = context.Description.HttpMethod;
    bool tip = IsTip(path, method);
    if (!tip && !IsMetered(path, method))
    {
      return;
    }

    IOpenApiSchema problem = await OpenApiProblemDetails.ReferenceAsync(context, cancellationToken).ConfigureAwait(false);
    if (tip)
    {
      DocumentTip(operation, ReadTipResource(context.ApplicationServices), problem);
    }
    else
    {
      DocumentMetered(operation, ReadMeteredResource(context.ApplicationServices), problem);
    }
  }

  private static bool IsTip(string path, string? method) =>
    string.Equals(path, TipRoute, StringComparison.OrdinalIgnoreCase)
    && (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
      || string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase));

  private static bool IsMetered(string path, string? method) =>
    string.Equals(path, MeteredRoute, StringComparison.OrdinalIgnoreCase)
    && string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);

  private static void DocumentTip(OpenApiOperation operation, string resource, IOpenApiSchema problem)
  {
    operation.Description = AppendNote
    (
      operation.Description,
      "Voluntary tip jar. Anonymous: no bearer. "
      + "Enabled unpaid payment returns 402 with PAYMENT-REQUIRED. "
      + "Disabled or misconfigured payment returns 503 and does not return 402. "
      + "Free and discovery routes never return 402. "
      + "Challenge resource.url is " + resource + ", including when the caller used the /api discovery alias."
    );
    DocumentPaymentRequired(operation, resource, problem, includeTip: true);
    DocumentUnavailable(operation, problem, includeTip: true);
  }

  private static void DocumentMetered(OpenApiOperation operation, string resource, IOpenApiSchema problem)
  {
    operation.Description = AppendNote
    (
      operation.Description,
      "Requires bearer scope " + AgentScopes.DemoInvoke + ". "
      + "The scope authorizes the attempt. Payment is separate: "
      + "402 with PAYMENT-REQUIRED when unpaid, or 200 with a ledger debit when credit or settlement covers the call. "
      + "Disabled or misconfigured payment returns 503 and does not return 402. "
      + "Free and discovery routes never return 402."
    );
    NoteSuccessDebit(operation);
    DocumentPaymentRequired(operation, resource, problem, includeTip: false);
    DocumentUnavailable(operation, problem, includeTip: false);
  }

  private static void NoteSuccessDebit(OpenApiOperation operation)
  {
    const string note =
      "Success debits the ledger. The body includes balanceAfter and fundingSource. "
      + "Prepaid credit or a settled x402 payment produces 200 instead of 402.";

    operation.Responses ??= new OpenApiResponses();
    if (operation.Responses.TryGetValue("200", out IOpenApiResponse? existing) && existing is OpenApiResponse success)
    {
      success.Description = AppendNote(success.Description, note);
      return;
    }

    operation.Responses["200"] = new OpenApiResponse { Description = note };
  }

  private static void DocumentPaymentRequired(OpenApiOperation operation, string resource, IOpenApiSchema problem, bool includeTip)
  {
    string description =
      "Payment is enabled and the call is unpaid or the payment signature was rejected. "
      + "The body is application/problem+json. "
      + "The PAYMENT-REQUIRED header is an x402 v2 challenge: base64 JSON with x402Version, "
      + "accepts (scheme, network, payTo, price), and resource.url. "
      + "Challenge resource.url is " + resource + ".";

    OpenApiResponse response = EnsureResponse(operation, "402", description);
    OpenApiProblemDetails.SetContent(response, PaymentRequiredSchema(problem, includeTip));

    response.Headers ??= new Dictionary<string, IOpenApiHeader>();
    if (response.Headers.TryGetValue(PaymentHeaders.PaymentRequired, out IOpenApiHeader? existing) && existing is OpenApiHeader header)
    {
      header.Required = true;
      header.Description = AppendNote(header.Description, ChallengeHeaderDescription);
      header.Schema ??= ChallengeHeaderSchema();
      return;
    }

    response.Headers[PaymentHeaders.PaymentRequired] = new OpenApiHeader
    {
      Required = true,
      Description = ChallengeHeaderDescription,
      Schema = ChallengeHeaderSchema(),
    };
  }

  private const string ChallengeHeaderDescription =
    "x402 v2 challenge. Base64-encoded JSON with x402Version, accepts (scheme, network, payTo, price), and resource.url.";

  private static OpenApiSchema ChallengeHeaderSchema() =>
    new()
    {
      Type = JsonSchemaType.String,
      ContentEncoding = "base64",
      ContentMediaType = "application/json",
      ContentSchema = new OpenApiSchema
      {
        Type = JsonSchemaType.Object,
        Description = "Decoded x402 v2 payment requirements.",
        Properties = new Dictionary<string, IOpenApiSchema>
        {
          ["x402Version"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
          ["accepts"] = new OpenApiSchema
          {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema
            {
              Type = JsonSchemaType.Object,
              Properties = new Dictionary<string, IOpenApiSchema>
              {
                ["scheme"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["network"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["payTo"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["price"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["description"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["mimeType"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["asset"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["maxTimeoutSeconds"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
              },
            },
          },
          ["resource"] = new OpenApiSchema
          {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
              ["url"] = new OpenApiSchema { Type = JsonSchemaType.String },
              ["description"] = new OpenApiSchema { Type = JsonSchemaType.String },
            },
          },
        },
      },
    };

  private static void DocumentUnavailable(OpenApiOperation operation, IOpenApiSchema problem, bool includeTip)
  {
    const string description =
      "Payment is disabled or misconfigured. The body is application/problem+json (error, payment, ok). "
      + "This response is not a 402 and carries no PAYMENT-REQUIRED header. "
      + "Free and discovery routes never return 402.";

    OpenApiResponse response = EnsureResponse(operation, "503", description);
    OpenApiProblemDetails.SetContent(response, UnavailableSchema(problem, includeTip));
  }

  /// <summary>
  /// SharedProblemDetails plus the 402 extension members the payment gate writes.
  /// </summary>
  private static OpenApiSchema PaymentRequiredSchema(IOpenApiSchema problem, bool includeTip)
  {
    Dictionary<string, IOpenApiSchema> members = new()
    {
      ["payment"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
      ["reason"] = new OpenApiSchema { Type = JsonSchemaType.String },
    };
    return WithMembers(problem, members, includeTip);
  }

  /// <summary>
  /// SharedProblemDetails plus the 503 extension members the payment gate writes.
  /// </summary>
  private static OpenApiSchema UnavailableSchema(IOpenApiSchema problem, bool includeTip)
  {
    Dictionary<string, IOpenApiSchema> members = new()
    {
      ["payment"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
      ["error"] = new OpenApiSchema { Type = JsonSchemaType.String },
      ["ok"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
    };
    return WithMembers(problem, members, includeTip);
  }

  private static OpenApiSchema WithMembers(IOpenApiSchema problem, Dictionary<string, IOpenApiSchema> members, bool includeTip)
  {
    if (includeTip)
    {
      members["tip"] = new OpenApiSchema { Type = JsonSchemaType.Boolean };
    }

    return new OpenApiSchema
    {
      AllOf =
      [
        problem,
        new OpenApiSchema { Type = JsonSchemaType.Object, Properties = members },
      ],
    };
  }

  private static OpenApiResponse EnsureResponse(OpenApiOperation operation, string statusCode, string description)
  {
    operation.Responses ??= new OpenApiResponses();
    OpenApiResponse response;
    if (operation.Responses.TryGetValue(statusCode, out IOpenApiResponse? existing) && existing is OpenApiResponse concrete)
    {
      response = concrete;
    }
    else
    {
      response = new OpenApiResponse();
      operation.Responses[statusCode] = response;
    }

    response.Description = AppendNote(response.Description, description);
    return response;
  }

  private static string Normalize(string? path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return string.Empty;
    }

    return path.Trim().TrimStart('/');
  }

  private static string ReadTipResource(IServiceProvider services) =>
    services.GetRequiredService<IOptions<TipOptions>>().Value.Resource;

  private static string ReadMeteredResource(IServiceProvider services) =>
    services.GetRequiredService<IOptions<MeteredCapabilityOptions>>().Value.Resource;

  private static string AppendNote(string? existing, string note)
  {
    if (string.IsNullOrEmpty(existing))
    {
      return note;
    }

    if (existing.Contains(note, StringComparison.Ordinal))
    {
      return existing;
    }

    return existing.TrimEnd() + " " + note;
  }
}
