#region Purpose
// One dictated clip for the Ask composer. The server forwards it to xAI speech-to-text.
#endregion

#region Design
// The browser records audio and sends base64. The key stays on the server, the same XAI:ApiKey
// and XAI:Endpoint the chat relay uses. The upstream call is POST {endpoint}/stt, multipart,
// Authorization: Bearer, with model then file last (docs.x.ai speech-to-text, 2026-11-24).
// MediaType may carry a codec parameter (audio/webm;codecs=opus); CanonicalMediaType strips it.
// Data is base64 with no data: prefix. A retry of the same bytes is safe to ask for, but it spends
// the key again and the transcript is not guaranteed identical, so this is ICommand.
// FakeTranscript is what the Development/Testing fake upstream and the mock factory return, so a
// browser proof can look for one sentence without a key.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = XaiChatDefaults.AuthenticatedPolicy,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class TranscribeAgentAudio
{
  public const int MaxDataLength = 6_000_000;

  /// <summary>Transcript the fake upstream and the mock factory return.</summary>
  public const string FakeTranscript = "Voice note.";

  public static readonly string[] MediaTypes =
  [
    "audio/webm",
    "audio/ogg",
    "audio/opus",
    "audio/wav",
    "audio/wave",
    "audio/x-wav",
    "audio/mp4",
    "audio/m4a",
    "audio/x-m4a",
    "audio/mpeg",
    "audio/mp3",
    "audio/flac",
    "audio/aac"
  ];

  /// <summary>Base media type without parameters, or null when it is not a supported audio type.</summary>
  public static string? CanonicalMediaType(string? mediaType)
  {
    if (string.IsNullOrWhiteSpace(mediaType))
    {
      return null;
    }

    string baseType = mediaType.Split(';')[0].Trim();
    foreach (string allowed in MediaTypes)
    {
      if (string.Equals(allowed, baseType, StringComparison.OrdinalIgnoreCase))
      {
        return allowed;
      }
    }

    return null;
  }

  [ApiRoute("api/agent-chat/transcriptions", HttpVerb.Post)]
  [Operation
  (
    "agent-chat.transcribe",
    Description = "Transcribe a dictated Ask prompt.",
    Group = "agent-chat"
  )]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public string MediaType { get; set; } = null!;

    public string Data { get; set; } = null!;
  }

  public sealed class Response : BaseResponse
  {
    public string Text { get; }

    public Response(string text)
    {
      Text = Guard.Against.NullOrWhiteSpace(text);
    }
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(command => command.MediaType)
        .NotEmpty()
        .Must(mediaType => CanonicalMediaType(mediaType) is not null)
        .WithMessage("MediaType must be a supported audio type.");

      RuleFor(command => command.Data)
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .MaximumLength(MaxDataLength)
        .Must(IsBase64)
        .WithMessage("Audio data must be base64.");
    }

    private static bool IsBase64(string data)
    {
      if (data.Length == 0 || data.Length % 4 != 0)
      {
        return false;
      }

      byte[] buffer = new byte[data.Length / 4 * 3];
      return Convert.TryFromBase64String(data, buffer, out int written) && written > 0;
    }
  }

  public static MockResponseFactory<Response> GetMockResponseFactory() =>
    static _ => new Response(FakeTranscript);
}
