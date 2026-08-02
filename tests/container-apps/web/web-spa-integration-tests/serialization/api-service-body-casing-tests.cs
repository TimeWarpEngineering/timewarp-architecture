#region Purpose
// Pins request-body casing at the contract seam: the SPA api service must serialize POST bodies
// with the seam options (camelCase), not compiler defaults (task 090's latent PascalCase leak).
#endregion

#region Design
// Black-box through the public GetResponse path: a capturing HttpMessageHandler stands in for the
// server, so the test exercises the real PrepareContent/transport code without exposing internals.
// ASP.NET Core's binder is case-insensitive, so no integration test can catch a casing regression —
// only a direct assertion on the wire body can. No Aspire host required.
#endregion

namespace ApiService_;

using System.Net;
using System.Net.Http;
using System.Text;
using TimeWarp.Architecture.Features.TodoItems;

public class RequestBody_Should
{
  private sealed class CapturingHandler : HttpMessageHandler
  {
    public string? CapturedBody;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      CapturedBody = request.Content is null
        ? null
        : await request.Content.ReadAsStringAsync(cancellationToken);

      string responseJson = JsonSerializer.Serialize(new CreateTodoItem.Response(), ContractSerializationDefaults.Options);
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
      };
    }
  }

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RequestBody_Should>();

  public static async Task Serialize_With_The_Seam_Options()
  {
    using var handler = new CapturingHandler();
    using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
    var apiService = new ApiServerApiService(httpClient, new MockAccessTokenProvider(), ContractSerializationDefaults.Options);

    var command = new CreateTodoItem.Command { ListId = 7, Title = "Seam check", Priority = 1 };

    await apiService.GetResponse<CreateTodoItem.Response>(command, CancellationToken.None);

    handler.CapturedBody.ShouldNotBeNull();
    handler.CapturedBody.ShouldContain("\"title\"", Case.Sensitive);
    handler.CapturedBody.ShouldContain("\"listId\"", Case.Sensitive);
    handler.CapturedBody.ShouldNotContain("\"Title\"", Case.Sensitive);
    handler.CapturedBody.ShouldNotContain("\"ListId\"", Case.Sensitive);
  }
}
