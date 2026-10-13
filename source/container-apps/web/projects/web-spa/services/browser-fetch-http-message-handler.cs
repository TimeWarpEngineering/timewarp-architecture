#region Purpose
// HttpMessageHandler that sends each request through the browser's fetch (web-authn.js SendJson), so
// the response's Set-Cookie lands in the browser in every render mode.
#endregion

#region Design
// Task 350: under InteractiveServer an HttpClient call made by an action handler is server→server
// (web-server's named self-call client), so a session issued on that response never reaches the
// browser. Putting this handler under HttpApiService keeps the contract transport (route, JSON body,
// 204 / problem mapping) in one place and only swaps the wire: the request goes out from the browser
// with its cookies and the browser stores the response's cookies. Used only for completions that
// issue the session (PasskeyCeremonyClient); every other call stays on IWebServerApiService.
// JSON bodies only: the ceremony contracts are JSON POSTs. Response headers other than Content-Type
// are not carried back (the browser already applied Set-Cookie; nothing else is read). Bodies are
// never logged — they carry ceremony material and the server's answer.
// [SideEffectService] (task 265): components never call this directly — TWA0026; action handlers do.
#endregion

namespace TimeWarp.Architecture.Services;

using System.Net.Http.Headers;

[SideEffectService]
internal sealed class BrowserFetchHttpMessageHandler : HttpMessageHandler
{
  private readonly IJSRuntime JsRuntime;

  public BrowserFetchHttpMessageHandler(IJSRuntime jsRuntime)
  {
    JsRuntime = jsRuntime;
  }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    Uri url = request.RequestUri
      ?? throw new InvalidOperationException("BrowserFetchHttpMessageHandler requires a request URI.");
    string? bodyJson = request.Content is null
      ? null
      : await request.Content.ReadAsStringAsync(cancellationToken);

    string envelopeJson =
      await WebAuthnJsModule.SendJsonAsync(
        JsRuntime,
        request.Method.Method,
        url.ToString(),
        bodyJson,
        cancellationToken);

    using var document = JsonDocument.Parse(envelopeJson);
    JsonElement root = document.RootElement;
    var response = new HttpResponseMessage((System.Net.HttpStatusCode)root.GetProperty("status").GetInt32())
    {
      RequestMessage = request,
      Content = new StringContent(root.GetProperty("body").GetString() ?? string.Empty)
    };

    if (root.TryGetProperty("contentType", out JsonElement contentType)
      && contentType.ValueKind == JsonValueKind.String
      && MediaTypeHeaderValue.TryParse(contentType.GetString(), out MediaTypeHeaderValue? mediaType))
    {
      response.Content.Headers.ContentType = mediaType;
    }

    return response;
  }
}
