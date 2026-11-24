#region Purpose
// Maps the Ask dictation WebSocket and hands the accepted socket to DictationRelay.
#endregion

#region Design
// TranscribeAgentAudio.StreamPath with a WebSocket upgrade: GET on HTTP/1.1, CONNECT on HTTP/2
// (RFC 8441; Chrome uses it over TLS, and MapGet alone answered 405). The same policy and schemes as
// the chat relay (identity-session and mock-identity-session, no agent bearer). The browser's
// session cookie is SameSite=Lax, so a cross-site page cannot open this socket as the signed-in
// person. A request that is not an upgrade is 400. The sample rate is read from the query and checked by the relay.
// The adapter turns WebSocket frames into DictationFrames: a binary message is audio (a message
// larger than the relay buffer is split across reads, which is fine for PCM), the text
// ClientMessages.Done is done, anything else or a close is closed. Sends are text. The socket is
// closed normally when the relay returns. UseWebSockets is in Program.ConfigureMiddleware.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TimeWarp.Architecture.Features.AgentChats.Application;

/// <summary>The dictation WebSocket endpoint.</summary>
public static class TranscribeAgentAudioEndpoint
{
  /// <summary>Maps <see cref="TranscribeAgentAudio.StreamPath"/>.</summary>
  public static void Map(IEndpointRouteBuilder endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    endpoints
      .MapMethods(TranscribeAgentAudio.StreamPath, [HttpMethods.Get, HttpMethods.Connect], HandleAsync)
      .RequireAuthorization
      (
        new AuthorizeAttribute
        {
          Policy = XaiChatDefaults.AuthenticatedPolicy,
          AuthenticationSchemes =
            AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
        }
      )
      .ExcludeFromDescription();
  }

  private static async Task HandleAsync(HttpContext httpContext, DictationRelay relay)
  {
    if (!httpContext.WebSockets.IsWebSocketRequest)
    {
      httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
      return;
    }

    int sampleRate = int.TryParse
    (
      httpContext.Request.Query[TranscribeAgentAudio.SampleRateQuery].ToString(),
      NumberStyles.None,
      CultureInfo.InvariantCulture,
      out int parsed
    )
      ? parsed
      : 0;
    using WebSocket socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
    CancellationToken aborted = httpContext.RequestAborted;
    await relay.RunAsync(new SocketChannel(socket), sampleRate, aborted).ConfigureAwait(false);
    if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
    {
      using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
      try
      {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token).ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is WebSocketException or OperationCanceledException)
      {
        // The browser already left.
      }
    }
  }

  private sealed class SocketChannel : IDictationChannel
  {
    private static readonly byte[] Done = Encoding.UTF8.GetBytes(TranscribeAgentAudio.ClientMessages.Done);

    private readonly WebSocket Socket;

    public SocketChannel(WebSocket socket)
    {
      Socket = socket;
    }

    public async ValueTask<DictationFrame> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
      ValueWebSocketReceiveResult result = await Socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
      if (result.MessageType == WebSocketMessageType.Binary)
      {
        return new DictationFrame(DictationFrameKind.Audio, result.Count);
      }

      if (result.MessageType == WebSocketMessageType.Text
        && result.EndOfMessage
        && buffer.Span[..result.Count].SequenceEqual(Done))
      {
        return new DictationFrame(DictationFrameKind.Done, 0);
      }

      return new DictationFrame(DictationFrameKind.Closed, 0);
    }

    public ValueTask SendAsync(string json, CancellationToken cancellationToken) =>
      Socket.SendAsync(Encoding.UTF8.GetBytes(json).AsMemory(), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
  }
}
