#region Purpose
// Narrated full walkthrough: keygen → register → token → whoami (executable documentation).
#endregion
#region Design
// Explains WHAT is signed and WHY (domain separation, one-time challenges, proof-of-possession)
// while actually performing each HTTP step. Suitable for a human reading along against `dev run`.
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("demo", Description = "Narrated full agent lifecycle walkthrough")]
internal sealed class DemoCommand : ICommand<Unit>
{
  [Option("server", "s", Description = "Identity server base URL")]
  public string? Server { get; set; }

  [Option("key-file", "k", Description = "Path for the PKCS#8 PEM private key")]
  public string? KeyFile { get; set; }

  [Option("force", "f", Description = "Overwrite an existing key file")]
  public bool Force { get; set; }

  internal sealed class Handler : ICommandHandler<DemoCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly PathDefaults Paths;
    private readonly AgentHttpClient Http;
    private readonly LocalKeyStore Store;

    public Handler(ITerminal terminal, PathDefaults paths, AgentHttpClient http, LocalKeyStore store)
    {
      Terminal = terminal;
      Paths = paths;
      Http = http;
      Store = store;
    }

    public async ValueTask<Unit> Handle(DemoCommand command, CancellationToken ct)
    {
      string server = string.IsNullOrWhiteSpace(command.Server) ? PathDefaults.DefaultServer : command.Server;
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;

      try
      {
        NarrateHeader(server, keyFile);

        // ── 1. keygen ──────────────────────────────────────────────────────
        Step("1/4  keygen — create a local ECDSA P-256 keypair");
        Terminal.WriteLine(
          "Agents authenticate without a browser. The only secret is a private key that never leaves this machine.");
        Terminal.WriteLine(
          "We export the public half as SPKI DER (openssl/WebCrypto/Python native format) and derive KeyId = SHA-256(SPKI).");

        GeneratedKey generated = AgentSigning.GenerateKey();
        AgentSigning.WriteKeyFile(keyFile, generated.Pem, command.Force);
        Terminal.WriteLine($"  wrote     {Path.GetFullPath(keyFile)}");
        Terminal.WriteLine($"  SPKI      {AgentSigning.ToBase64Url(generated.SpkiPublicKey)}");
        Terminal.WriteLine($"  KeyId     {AgentSigning.ToBase64Url(generated.KeyId)}");

        using LoadedKey key = AgentSigning.LoadKey(keyFile);

        // ── 2. register ────────────────────────────────────────────────────
        Step("2/4  register — prove possession of the NEW key (Register.v1)");
        Terminal.WriteLine(
          "The server mints a one-time challenge. We sign UTF8(\"TimeWarp.Identity.AgentKey.Register.v1:\") ‖ challenge.");
        Terminal.WriteLine(
          "Domain separation: a registration signature can never verify as a token signature (different prefix).");

        Terminal.WriteLine($"  POST {AgentHttpClient.RegisterOptionsPath}");
        HttpResult<ChallengeResponse> regOptions = await Http.PostRegisterOptionsAsync(server, ct).ConfigureAwait(false);
        if (!regOptions.Success || regOptions.Value is null)
        {
          return FailHttp(regOptions.StatusCode, regOptions.RawBody);
        }

        Terminal.WriteLine($"  challenge {regOptions.Value.Challenge}");
        byte[] regChallenge = AgentSigning.FromBase64Url(regOptions.Value.Challenge);
        byte[] regSig = AgentSigning.Sign(key.Ecdsa, AgentKeyCeremonyType.Registration, regChallenge);

        var regRequest = new RegisterRequest
        {
          PublicKey = AgentSigning.ToBase64Url(key.SpkiPublicKey),
          Challenge = regOptions.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(regSig),
          Label = "agent-identity-cli-demo"
        };

        Terminal.WriteLine($"  POST {AgentHttpClient.RegisterPath}");
        HttpResult<RegisterResponse> reg = await Http.PostRegisterAsync(server, regRequest, ct).ConfigureAwait(false);
        if (!reg.Success || reg.Value is null)
        {
          return FailHttp(reg.StatusCode, reg.RawBody);
        }

        Store.UpdateRegistration(keyFile, reg.Value.PrincipalId, reg.Value.KeyId);
        Terminal.WriteLine($"  PrincipalId {reg.Value.PrincipalId}".Green());
        Terminal.WriteLine($"  KeyId       {reg.Value.KeyId}".Green());

        // ── 3. token ───────────────────────────────────────────────────────
        Step("3/4  token — prove possession again for a bearer (Token.v1)");
        Terminal.WriteLine(
          "Token issuance uses a DIFFERENT ceremony type and prefix: \"TimeWarp.Identity.AgentKey.Token.v1:\".");
        Terminal.WriteLine(
          "The challenge store is one-time per ceremony type — replaying either ceremony fails.");
        Terminal.WriteLine(
          $"Default scope {AgentScopes.IdentityRead} gates GET /api/identity/agent/me.");

        Terminal.WriteLine($"  POST {AgentHttpClient.TokenOptionsPath}");
        HttpResult<ChallengeResponse> tokOptions = await Http.PostTokenOptionsAsync(server, ct).ConfigureAwait(false);
        if (!tokOptions.Success || tokOptions.Value is null)
        {
          return FailHttp(tokOptions.StatusCode, tokOptions.RawBody);
        }

        Terminal.WriteLine($"  challenge {tokOptions.Value.Challenge}");
        byte[] tokChallenge = AgentSigning.FromBase64Url(tokOptions.Value.Challenge);
        byte[] tokSig = AgentSigning.Sign(key.Ecdsa, AgentKeyCeremonyType.TokenIssuance, tokChallenge);

        var tokRequest = new TokenRequest
        {
          KeyId = reg.Value.KeyId,
          Challenge = tokOptions.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(tokSig),
          Scopes = [AgentScopes.IdentityRead]
        };

        Terminal.WriteLine($"  POST {AgentHttpClient.TokenPath}");
        HttpResult<TokenResponse> tok = await Http.PostTokenAsync(server, tokRequest, ct).ConfigureAwait(false);
        if (!tok.Success || tok.Value is null)
        {
          return FailHttp(tok.StatusCode, tok.RawBody);
        }

        Store.UpdateToken(keyFile, tok.Value.AccessToken, tok.Value.TokenType, tok.Value.ExpiresInSeconds, tok.Value.Scopes, tok.Value.PrincipalId);

        Terminal.WriteLine($"  tokenType  {tok.Value.TokenType}".Green());
        Terminal.WriteLine($"  expiresIn  {tok.Value.ExpiresInSeconds}s".Green());
        Terminal.WriteLine($"  scopes     {string.Join(", ", tok.Value.Scopes)}".Green());
        Terminal.WriteLine($"  access     {tok.Value.AccessToken}");

        // ── 4. whoami ──────────────────────────────────────────────────────
        Step("4/4  whoami — call a protected resource with the bearer");
        Terminal.WriteLine(
          "Authorization: Bearer <accessToken>. The server validates the opaque token and requires identity:read.");

        Terminal.WriteLine($"  GET {AgentHttpClient.MePath}");
        HttpResult<WhoAmIResponse> me = await Http.GetMeAsync(server, tok.Value.AccessToken, ct).ConfigureAwait(false);
        if (!me.Success || me.Value is null)
        {
          return FailHttp(me.StatusCode, me.RawBody);
        }

        Terminal.WriteLine($"  PrincipalId {me.Value.PrincipalId}".Green());
        Terminal.WriteLine($"  Kind        {me.Value.Kind}".Green());
        Terminal.WriteLine($"  TrustTier   {me.Value.TrustTier}".Green());
        Terminal.WriteLine($"  Scopes      {string.Join(", ", me.Value.Scopes)}".Green());

        Terminal.WriteLine();
        Terminal.WriteLine("Demo complete. You just walked the full agent lifecycle.".Green());
        Terminal.WriteLine($"Store sidecar: {PathDefaults.ResolveStorePath(keyFile)}");
        Terminal.WriteLine("Next: copy tools/agent-identity-cli as a reference client, or re-run individual commands.");
      }
      catch (Exception ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
      }

      return Value;
    }

    private void NarrateHeader(string server, string keyFile)
    {
      Terminal.WriteLine("══════════════════════════════════════════════════════════════");
      Terminal.WriteLine(" Agent identity ceremony demo (task 104-029)");
      Terminal.WriteLine("══════════════════════════════════════════════════════════════");
      Terminal.WriteLine($"Server   : {server}");
      Terminal.WriteLine($"Key file : {Path.GetFullPath(keyFile)}");
      Terminal.WriteLine("Library  : AgentKeyProof.BuildSignedData (TimeWarp.Identity) — DER ECDSA P-256");
      Terminal.WriteLine();
    }

    private void Step(string title)
    {
      Terminal.WriteLine();
      Terminal.WriteLine($"── {title} ──");
    }

    private Unit FailHttp(int status, string body)
    {
      Terminal.WriteErrorLine($"HTTP {status}".Red());
      Terminal.WriteErrorLine(body);
      Environment.ExitCode = 1;
      return Value;
    }
  }
}
