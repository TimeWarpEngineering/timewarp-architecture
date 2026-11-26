#region Purpose
// Documents HTTP 402, 503, and the x402 request and settlement headers on the tip jar and metered
// capability, and states that free routes never return 402.
#endregion

#region Design
// FastEndpoints infers the success body, so the document otherwise shows only 200.
// 402 and 503 are SharedProblemDetails plus, on 402 only, the PAYMENT-REQUIRED header. Each body
// schema is allOf: a $ref to the one SharedProblemDetails component (OpenApiProblemDetails), then
// the extension members the payment gate adds (payment, reason or error and ok, tip on the tip jar).
// That header is base64 x402 v2 JSON (x402Version, accepts, resource), so the header schema
// carries contentEncoding base64 and the decoded object as contentSchema.
// Each paid operation also lists the optional request header PAYMENT-SIGNATURE (the client's
// signed x402 payload) and the optional PAYMENT-RESPONSE header on 200 (the settlement result,
// present when an x402 payment settled), both named from PaymentHeaders.
// Routes come from the contract RouteTemplate constants. The bare /api alias is a rewrite
// onto the tip jar. OpenApiDiscoveryCoverage copies the tip operations' responses and
// parameters onto it after these operation transformers run, so it carries the same 402 and 503.
// Free and discovery operations are left without
// 402 or 503. Challenge resource.url is TipOptions.Resource or MeteredCapabilityOptions.Resource,
// read from the host services, not a second copy of the route.
// info.description gets the payment note as its own paragraph. The "documented only on" list is
// read from the finished document (every operation with a 402), and every other operation with a
// 503 (GET /api/health is Unhealthy) is named as a non-payment 503, so the note cannot drift from
// the operations. This document transformer runs after OpenApiDiscoveryCoverage's, which adds the
// alias and health operations.
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

  internal const string PaymentHeading = "## Payment";

  private const string SignatureDescription =
    "x402 v2 signed payment payload, base64-encoded JSON. Optional: send it to pay for this call "
    + "after a 402 challenge. A rejected signature returns 402 again.";

  private const string SettlementDescription =
    "x402 v2 settlement result, base64-encoded JSON. Present when an x402 payment settled for this call.";

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
    string? existing = document.Info.Description;
    if (existing?.Contains(PaymentHeading, StringComparison.Ordinal) == true)
    {
      return Task.CompletedTask;
    }

    string note = PaymentNote(document);
    document.Info.Description = string.IsNullOrWhiteSpace(existing)
      ? note
      : existing.TrimEnd() + "\n\n" + note;
    return Task.CompletedTask;
  }

  /// <summary>
  /// The info.description payment paragraph, built from the operations in the finished document.
  /// </summary>
  private static string PaymentNote(OpenApiDocument document)
  {
    List<string> paid = [];
    List<string> otherUnavailable = [];
    foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths ?? [])
    {
      if (path.Value.Operations is null)
      {
        continue;
      }

      foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in path.Value.Operations)
      {
        OpenApiResponses? responses = operation.Value.Responses;
        string name = "`" + operation.Key.Method.ToUpperInvariant() + " " + path.Key + "`";
        if (responses?.ContainsKey("402") == true)
        {
          paid.Add(name);
        }
        else if (responses?.ContainsKey("503") == true)
        {
          otherUnavailable.Add(name);
        }
      }
    }

    string note =
      PaymentHeading + "\n\n"
      + "Free and discovery operations never return HTTP 402. "
      + "HTTP 402 and the payment HTTP 503 are documented only on " + JoinNames(paid) + ". "
      + "This document, health, identity ceremonies, and other free routes are not payment challenges. "
      + "The bare /api path is a discovery alias of the tip jar and carries the tip jar's responses. "
      + "Disabled or misconfigured payment returns 503 on those paid routes and does not return 402. "
      + "A paid call may send " + PaymentHeaders.PaymentSignature + " and a settled 200 carries "
      + PaymentHeaders.PaymentResponse + ".";
    if (otherUnavailable.Count > 0)
    {
      note += " The 503 on " + JoinNames(otherUnavailable)
        + " is that route's own status (such as Unhealthy), not a payment response.";
    }

    return note;
  }

  private static string JoinNames(List<string> names) =>
    names.Count switch
    {
      0 => "no operation",
      1 => names[0],
      _ => string.Join(", ", names[..^1]) + " and " + names[^1],
    };

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
    DocumentPaymentHeaders(operation);
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
    DocumentPaymentHeaders(operation);
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

  /// <summary>
  /// Optional PAYMENT-SIGNATURE on the request and PAYMENT-RESPONSE on the 200.
  /// </summary>
  private static void DocumentPaymentHeaders(OpenApiOperation operation)
  {
    operation.Parameters ??= [];
    bool hasSignature = operation.Parameters.Any
    (
      static parameter => parameter.In == ParameterLocation.Header
        && string.Equals(parameter.Name, PaymentHeaders.PaymentSignature, StringComparison.OrdinalIgnoreCase)
    );
    if (!hasSignature)
    {
      operation.Parameters.Add
      (
        new OpenApiParameter
        {
          Name = PaymentHeaders.PaymentSignature,
          In = ParameterLocation.Header,
          Required = false,
          Description = SignatureDescription,
          Schema = Base64JsonSchema(),
        }
      );
    }

    operation.Responses ??= new OpenApiResponses();
    if (!operation.Responses.TryGetValue("200", out IOpenApiResponse? existing) || existing is not OpenApiResponse success)
    {
      success = new OpenApiResponse { Description = "Success." };
      operation.Responses["200"] = success;
    }

    success.Headers ??= new Dictionary<string, IOpenApiHeader>();
    if (!success.Headers.ContainsKey(PaymentHeaders.PaymentResponse))
    {
      success.Headers[PaymentHeaders.PaymentResponse] = new OpenApiHeader
      {
        Required = false,
        Description = SettlementDescription,
        Schema = Base64JsonSchema(),
      };
    }
  }

  private static OpenApiSchema Base64JsonSchema() =>
    new()
    {
      Type = JsonSchemaType.String,
      ContentEncoding = "base64",
      ContentMediaType = "application/json",
    };

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
