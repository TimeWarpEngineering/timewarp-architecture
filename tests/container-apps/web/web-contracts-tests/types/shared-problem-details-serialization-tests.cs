#region Purpose
// Proves SharedProblemDetails round-trips server ProblemDetails payloads losslessly.
#endregion

#region Design
// The type's contract (see its Design region) is lossless interchange with
// Microsoft.AspNetCore.Mvc.ProblemDetails — the validation "errors" dictionary must survive via
// the Extensions extension-data catch-all, since that is what the SPA renders for 400 responses.
#endregion

// ReSharper disable InconsistentNaming
namespace SharedProblemDetails_;

using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Foundation.Types;

public class SharedProblemDetails_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<SharedProblemDetails_Should>();

  public static Task SerializeAndDeserialize_Preserving_Extensions()
  {
    string serverPayload =
      """
      {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "One or more validation errors occurred.",
        "status": 400,
        "detail": "See errors.",
        "instance": "/api/Roles",
        "errors": { "Name": ["'Name' must not be empty."] },
        "traceId": "00-abc-def-00"
      }
      """;

    SharedProblemDetails? parsed =
      JsonSerializer.Deserialize<SharedProblemDetails>(serverPayload, ContractSerialization.Options);

    parsed.ShouldNotBeNull();
    parsed.Status.ShouldBe(400);
    parsed.Title.ShouldBe("One or more validation errors occurred.");
    parsed.Extensions.ShouldContainKey("errors");
    parsed.Extensions.ShouldContainKey("traceId");

    // And back out: what the client re-serializes must still carry the extension data.
    string reserialized = JsonSerializer.Serialize(parsed, ContractSerialization.Options);
    reserialized.ShouldContain("errors");
    reserialized.ShouldContain("traceId");
    return Task.CompletedTask;
  }
}
