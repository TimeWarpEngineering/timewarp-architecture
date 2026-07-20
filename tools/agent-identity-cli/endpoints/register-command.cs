#region Purpose
// Walk the agent key registration ceremony: options → sign Register.v1 → complete.
#endregion
#region Design
// Signs UTF8("TimeWarp.Identity.AgentKey.Register.v1:") ‖ challenge via AgentKeyProof.BuildSignedData
// + ECDsa DER. Persists principalId/keyId to the sidecar store next to the key file.
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("register", Description = "Register the local agent public key with the server")]
internal sealed class RegisterCommand : ICommand<Unit>
{
  [Option("server", "s", Description = "Identity server base URL")]
  public string? Server { get; set; }

  [Option("key-file", "k", Description = "Path to the PKCS#8 PEM private key")]
  public string? KeyFile { get; set; }

  [Option("label", "l", Description = "Optional cosmetic label for the credential")]
  public string? Label { get; set; }

  internal sealed class Handler : ICommandHandler<RegisterCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly PathDefaults Paths;
    private readonly AgentSigning Signing;
    private readonly AgentHttpClient Http;
    private readonly LocalKeyStore Store;

    public Handler(ITerminal terminal, PathDefaults paths, AgentSigning signing, AgentHttpClient http, LocalKeyStore store)
    {
      Terminal = terminal;
      Paths = paths;
      Signing = signing;
      Http = http;
      Store = store;
    }

    public async ValueTask<Unit> Handle(RegisterCommand command, CancellationToken ct)
    {
      string server = string.IsNullOrWhiteSpace(command.Server) ? PathDefaults.DefaultServer : command.Server;
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;

      try
      {
        using LoadedKey key = Signing.LoadKey(keyFile);

        Terminal.WriteLine($"POST {server}{AgentHttpClient.RegisterOptionsPath}");
        HttpResult<ChallengeResponse> options = await Http.PostRegisterOptionsAsync(server, ct).ConfigureAwait(false);
        if (!options.Success || options.Value is null)
        {
          WriteHttpError(options.StatusCode, options.RawBody);
          return Value;
        }

        byte[] challengeBytes = AgentSigning.FromBase64Url(options.Value.Challenge);
        byte[] signature = Signing.Sign(key.Ecdsa, AgentKeyCeremonyType.Registration, challengeBytes);

        var request = new RegisterRequest
        {
          PublicKey = AgentSigning.ToBase64Url(key.SpkiPublicKey),
          Challenge = options.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(signature),
          Label = string.IsNullOrWhiteSpace(command.Label) ? null : command.Label
        };

        Terminal.WriteLine($"POST {server}{AgentHttpClient.RegisterPath} (Register.v1 signature)");
        HttpResult<RegisterResponse> complete = await Http.PostRegisterAsync(server, request, ct).ConfigureAwait(false);
        if (!complete.Success || complete.Value is null)
        {
          WriteHttpError(complete.StatusCode, complete.RawBody);
          return Value;
        }

        Store.UpdateRegistration(keyFile, complete.Value.PrincipalId, complete.Value.KeyId);

        Terminal.WriteLine("Registration succeeded.".Green());
        Terminal.WriteLine($"PrincipalId : {complete.Value.PrincipalId}");
        Terminal.WriteLine($"KeyId       : {complete.Value.KeyId}");
        Terminal.WriteLine($"Store       : {PathDefaults.ResolveStorePath(keyFile)}");
      }
      catch (Exception ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
      }

      return Value;
    }

    private void WriteHttpError(int status, string body)
    {
      Terminal.WriteErrorLine($"HTTP {status}".Red());
      Terminal.WriteErrorLine(body);
      Environment.ExitCode = 1;
    }
  }
}
