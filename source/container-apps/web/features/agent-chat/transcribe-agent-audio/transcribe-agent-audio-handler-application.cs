#region Purpose
// Admits one transcription for the current principal and forwards it.
#endregion

#region Design
// The endpoint policy already requires a principal. The handler checks again so a miswired
// endpoint is not an open proxy to the key. A missing key returns 503 with the environment's
// setup hint and does not call the provider. BeginKeyResolution runs before that check so the
// transcribe reuses the same read. Admission is the chat relay's cap: a dictation spends the
// same key. The lease is disposed on every path, including cancellation.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

using Microsoft.Extensions.Hosting;
using static TimeWarp.Architecture.Features.AgentChats.TranscribeAgentAudio;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class TranscribeAgentAudio
{
  /// <summary>Forwards an admitted clip to <see cref="IAgentSpeechToText"/>.</summary>
  public sealed class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IAgentSpeechToText Speech;
    private readonly AgentChatAdmission Admission;
    private readonly IHostEnvironment Environment;

    /// <summary>Creates the handler.</summary>
    public Handler
    (
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IAgentSpeechToText speech,
      AgentChatAdmission admission,
      IHostEnvironment environment
    )
    {
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      Speech = speech;
      Admission = admission;
      Environment = environment;
    }

    /// <summary>Returns the transcript, or a problem.</summary>
    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle
    (
      Command request,
      CancellationToken cancellationToken
    )
    {
      PrincipalId? principalId = await CurrentPrincipalAccessor
        .GetCurrentPrincipalIdAsync(cancellationToken)
        .ConfigureAwait(false);
      if (principalId is null)
      {
        return AgentChatProblems.Unauthenticated();
      }

      Speech.BeginKeyResolution();
      if (!await Speech.IsConfiguredAsync(cancellationToken).ConfigureAwait(false))
      {
        return AgentChatProblems.NotConfigured(Speech.MissingKeyDetail(Environment.IsDevelopment()));
      }

      using AgentChatAdmission.AdmissionLease lease = await Admission
        .TryAdmitAsync(principalId.Value.Value.ToString("D"), cancellationToken)
        .ConfigureAwait(false);
      if (!lease.Admitted)
      {
        return AgentChatProblems.TooManyRequests();
      }

      return await Speech.TranscribeAsync(request, cancellationToken).ConfigureAwait(false);
    }
  }
}
