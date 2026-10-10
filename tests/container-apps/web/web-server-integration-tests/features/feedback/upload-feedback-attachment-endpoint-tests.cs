#region Purpose
// HTTP proof that a feedback upload accepts an allow-listed media type other than JSON.
#endregion

#region Design
// FastEndpoints accepts only application/json on a POST request DTO. The host then selects
// its empty 415 endpoint before UploadFeedbackAttachmentEndpoint runs, which is what CI saw
// for notes.txt (text/plain, 21 bytes, 0-byte response). This posts through the real host
// with the file's own Content-Type. A new passkey account has feedback.file.self. The
// disallowed type stays on the host 415. The handler's magic-byte 415 is covered by the
// co-located Jaribu runfile. The client accepts the loopback dev certificate; CI runners
// do not trust it.
#endregion

namespace UploadFeedbackAttachmentEndpoint_;

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Foundation.Features;

public class Accepts_
{
  private static readonly byte[] Png = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;
  private static string? SessionCookie;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Accepts_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
    (_, SessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task TextPlain_Given_Notes()
  {
    (HttpStatusCode status, byte[] body) = await PostAsync(
      "notes.txt",
      new MediaTypeHeaderValue(FeedbackAttachmentRules.Text),
      "hello from the picker"u8.ToArray());

    status.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(body);
    document.RootElement.GetProperty("fileName").GetString().ShouldBe("notes.txt");
    document.RootElement.GetProperty("contentType").GetString().ShouldBe(FeedbackAttachmentRules.Text);
    document.RootElement.GetProperty("size").GetInt64().ShouldBe(21);
  }

  public static async Task TextPlain_Given_Charset()
  {
    MediaTypeHeaderValue contentType = new(FeedbackAttachmentRules.Text) { CharSet = "utf-8" };
    (HttpStatusCode status, byte[] body) = await PostAsync("notes.txt", contentType, "hello"u8.ToArray());

    status.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(body);
    document.RootElement.GetProperty("contentType").GetString().ShouldBe(FeedbackAttachmentRules.Text);
  }

  public static async Task Png_Given_Image()
  {
    (HttpStatusCode status, byte[] body) = await PostAsync(
      "shot.png",
      new MediaTypeHeaderValue(FeedbackAttachmentRules.Png),
      Png);

    status.ShouldBe(HttpStatusCode.OK);
    using JsonDocument document = JsonDocument.Parse(body);
    document.RootElement.GetProperty("contentType").GetString().ShouldBe(FeedbackAttachmentRules.Png);
  }

  public static async Task Svg_Given_DisallowedType_Should_BeEmpty415()
  {
    (HttpStatusCode status, byte[] body) = await PostAsync(
      "picture.svg",
      new MediaTypeHeaderValue("image/svg+xml"),
      "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray());

    status.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    body.Length.ShouldBe(0);
  }

  private static async Task<(HttpStatusCode Status, byte[] Body)> PostAsync(
    string fileName,
    MediaTypeHeaderValue contentType,
    byte[] body)
  {
    SessionCookie.ShouldNotBeNull();
    using HttpClient client = new(new HttpClientHandler
    {
      ServerCertificateCustomValidationCallback = (message, _, _, errors) =>
        errors == System.Net.Security.SslPolicyErrors.None || message.RequestUri?.IsLoopback == true
    })
    {
      BaseAddress = Web.HttpClient.BaseAddress
    };
    using HttpRequestMessage request = new(HttpMethod.Post, UploadFeedbackAttachment.Command.RouteTemplate);
    ByteArrayContent content = new(body);
    content.Headers.ContentType = contentType;
    request.Content = content;
    request.Headers.TryAddWithoutValidation(FileUploadHeaders.FileName, Uri.EscapeDataString(fileName));
    request.Headers.TryAddWithoutValidation("Cookie", SessionCookie);
    using HttpResponseMessage response = await client.SendAsync(request);
    byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
    return (response.StatusCode, responseBody);
  }
}
