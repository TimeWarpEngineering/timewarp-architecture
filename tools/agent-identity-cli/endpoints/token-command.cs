#region Purpose
// Walk the agent token issuance ceremony: options → sign Token.v1 → complete.
#endregion
#region Design
// Domain-separated from registration (AgentKeyCeremonyType.TokenIssuance / Token.v1 prefix).
// Default scope identity:read matches the GetAgentIdentity policy. Bearer is stored in the sidecar.
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("token", Description = "Prove possession of a registered key and obtain a bearer token")]
internal sealed class TokenCommand : ICommand<Unit>
{
  [Option("server", "s", Description = "Identity server base URL")]
  public string? Server { get; set; }

  [Option("key-file", "k", Description = "Path to the PKCS#8 PEM private key")]
  public string? KeyFile { get; set; }

  [Option("scopes", null, Description = "Comma-separated scopes (default: identity:read)")]
  public string? Scopes { get; set; }

  internal sealed class Handler : ICommandHandler<TokenCommand, Unit>
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

    public async ValueTask<Unit> Handle(TokenCommand command, CancellationToken ct)
    {
      string server = string.IsNullOrWhiteSpace(command.Server) ? PathDefaults.DefaultServer : command.Server;
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;
      List<string> scopes = ParseScopes(command.Scopes);

      try
      {
        AgentStoreRecord? record = Store.TryLoad(keyFile);
        using LoadedKey key = Signing.LoadKey(keyFile);
        string keyId = string.IsNullOrWhiteSpace(record?.KeyId)
          ? AgentSigning.ToBase64Url(key.KeyId)
          : record!.KeyId!;
        if (string.IsNullOrWhiteSpace(record?.KeyId))
        {
          Terminal.WriteLine($"No KeyId in store; using local KeyId {keyId}");
        }

        Terminal.WriteLine($"POST {server}{AgentHttpClient.TokenOptionsPath}");
        HttpResult<ChallengeResponse> options = await Http.PostTokenOptionsAsync(server, ct).ConfigureAwait(false);
        if (!options.Success || options.Value is null)
        {
          WriteHttpError(options.StatusCode, options.RawBody);
          return Value;
        }

        byte[] challengeBytes = AgentSigning.FromBase64Url(options.Value.Challenge);
        byte[] signature = Signing.Sign(key.Ecdsa, AgentKeyCeremonyType.TokenIssuance, challengeBytes);

        var request = new TokenRequest
        {
          KeyId = keyId,
          Challenge = options.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(signature),
          Scopes = scopes
        };

        Terminal.WriteLine($"POST {server}{AgentHttpClient.TokenPath} (Token.v1 signature)");
        HttpResult<TokenResponse> complete = await Http.PostTokenAsync(server, request, ct).ConfigureAwait(false);
        if (!complete.Success || complete.Value is null)
        {
          WriteHttpError(complete.StatusCode, complete.RawBody);
          return Value;
        }

        TokenResponse token = complete.Value;
        Store.UpdateToken(keyFile, token.AccessToken, token.TokenType, token.ExpiresInSeconds, token.Scopes, token.PrincipalId);
        // Ensure KeyId is present even when the store was empty before this call.
        AgentStoreRecord? updated = Store.TryLoad(keyFile);
        if (updated is not null && string.IsNullOrWhiteSpace(updated.KeyId))
        {
          updated.KeyId = keyId;
          if (string.IsNullOrWhiteSpace(updated.PrincipalId))
          {
            updated.PrincipalId = token.PrincipalId;
          }

          Store.Save(keyFile, updated);
        }

        Terminal.WriteLine("Token issued.".Green());
        Terminal.WriteLine($"Token type : {token.TokenType}");
        Terminal.WriteLine($"Expires in : {token.ExpiresInSeconds}s");
        Terminal.WriteLine($"Scopes     : {string.Join(", ", token.Scopes)}");
        Terminal.WriteLine($"Principal  : {token.PrincipalId}");
        Terminal.WriteLine($"Access     : {token.AccessToken}");
      }
      catch (Exception ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
      }

      return Value;
    }

    private static List<string> ParseScopes(string? scopesOption)
    {
      if (string.IsNullOrWhiteSpace(scopesOption))
      {
        return [AgentScopes.IdentityRead];
      }

      return scopesOption
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(s => s.Length > 0)
        .ToList();
    }

    private void WriteHttpError(int status, string body)
    {
      Terminal.WriteErrorLine($"HTTP {status}".Red());
      Terminal.WriteErrorLine(body);
      Environment.ExitCode = 1;
    }
  }
}
