#region Purpose
// Narrated agent money path (104-014): keygen → register → token(demo:invoke) → unpaid 402 → optional pay → whoami Funded.
#endregion
#region Design
// Executable documentation for the no-human agent commerce loop. Live settle needs a real x402
// PAYMENT-SIGNATURE (wallet/facilitator). CI owns the mock-facilitator continuous path in
// invoke-metered-capability-tests.cs Money_Path_E2E_*. Quota is server-side ledger balance after
// settle/credit — not a JWT claim on the opaque bearer (same token works before and after pay).
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("money-path", Description = "Narrated agent money path: register → 402 → pay → metered call")]
internal sealed class MoneyPathCommand : ICommand<Unit>
{
  [Option("server", "s", Description = "Identity / metered server base URL")]
  public string? Server { get; set; }

  [Option("key-file", "k", Description = "Path for the PKCS#8 PEM private key")]
  public string? KeyFile { get; set; }

  [Option("force", "f", Description = "Overwrite an existing key file")]
  public bool Force { get; set; }

  [Option("payment-signature", null, Description = "Base64 x402 PAYMENT-SIGNATURE for the paid retry (live settle)")]
  public string? PaymentSignature { get; set; }

  internal sealed class Handler : ICommandHandler<MoneyPathCommand, Unit>
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

    public async ValueTask<Unit> Handle(MoneyPathCommand command, CancellationToken ct)
    {
      string server = string.IsNullOrWhiteSpace(command.Server) ? PathDefaults.DefaultServer : command.Server;
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;

      try
      {
        NarrateHeader(server, keyFile);

        // ── 1. keygen ──────────────────────────────────────────────────────
        Step("1/6  keygen — local ECDSA P-256 keypair (secret never leaves this machine)");
        GeneratedKey generated = AgentSigning.GenerateKey();
        AgentSigning.WriteKeyFile(keyFile, generated.Pem, command.Force);
        Terminal.WriteLine($"  wrote  {Path.GetFullPath(keyFile)}");
        Terminal.WriteLine($"  KeyId  {AgentSigning.ToBase64Url(generated.KeyId)}");
        using LoadedKey key = AgentSigning.LoadKey(keyFile);

        // ── 2. register ────────────────────────────────────────────────────
        Step("2/6  register — Agent principal, no human sponsor (Register.v1 proof)");
        HttpResult<ChallengeResponse> regOptions = await Http.PostRegisterOptionsAsync(server, ct).ConfigureAwait(false);
        if (!regOptions.Success || regOptions.Value is null)
        {
          return FailHttp(regOptions.StatusCode, regOptions.RawBody);
        }

        byte[] regChallenge = AgentSigning.FromBase64Url(regOptions.Value.Challenge);
        byte[] regSig = AgentSigning.Sign(key.Ecdsa, AgentKeyCeremonyType.Registration, regChallenge);
        var regRequest = new RegisterRequest
        {
          PublicKey = AgentSigning.ToBase64Url(key.SpkiPublicKey),
          Challenge = regOptions.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(regSig),
          Label = "agent-identity-cli-money-path"
        };
        HttpResult<RegisterResponse> reg = await Http.PostRegisterAsync(server, regRequest, ct).ConfigureAwait(false);
        if (!reg.Success || reg.Value is null)
        {
          return FailHttp(reg.StatusCode, reg.RawBody);
        }

        Store.UpdateRegistration(keyFile, reg.Value.PrincipalId, reg.Value.KeyId);
        Terminal.WriteLine($"  PrincipalId {reg.Value.PrincipalId}".Green());
        Terminal.WriteLine($"  KeyId       {reg.Value.KeyId}".Green());
        Terminal.WriteLine("  TrustTier   Keyed (after first credential)");

        // ── 3. token with demo:invoke ──────────────────────────────────────
        Step("3/6  token — scopes demo:invoke + identity:read (Token.v1)");
        Terminal.WriteLine(
          "demo:invoke authorizes the attempt; payment is separate (402/credit). identity:read enables whoami.");
        HttpResult<ChallengeResponse> tokOptions = await Http.PostTokenOptionsAsync(server, ct).ConfigureAwait(false);
        if (!tokOptions.Success || tokOptions.Value is null)
        {
          return FailHttp(tokOptions.StatusCode, tokOptions.RawBody);
        }

        byte[] tokChallenge = AgentSigning.FromBase64Url(tokOptions.Value.Challenge);
        byte[] tokSig = AgentSigning.Sign(key.Ecdsa, AgentKeyCeremonyType.TokenIssuance, tokChallenge);
        var tokRequest = new TokenRequest
        {
          KeyId = reg.Value.KeyId,
          Challenge = tokOptions.Value.Challenge,
          Signature = AgentSigning.ToBase64Url(tokSig),
          Scopes = [AgentScopes.DemoInvoke, AgentScopes.IdentityRead]
        };
        HttpResult<TokenResponse> tok = await Http.PostTokenAsync(server, tokRequest, ct).ConfigureAwait(false);
        if (!tok.Success || tok.Value is null)
        {
          return FailHttp(tok.StatusCode, tok.RawBody);
        }

        Store.UpdateToken(
          keyFile,
          tok.Value.AccessToken,
          tok.Value.TokenType,
          tok.Value.ExpiresInSeconds,
          tok.Value.Scopes,
          tok.Value.PrincipalId);
        Terminal.WriteLine($"  scopes  {string.Join(", ", tok.Value.Scopes)}".Green());
        Terminal.WriteLine($"  access  {tok.Value.AccessToken}");

        // ── 4. unpaid metered call → 402 ───────────────────────────────────
        Step("4/6  unpaid GET metered-capability — expect 402 + PAYMENT-REQUIRED");
        Terminal.WriteLine($"  GET {AgentHttpClient.MeteredCapabilityPath}");
        MeteredHttpResult unpaid = await Http
          .GetMeteredAsync(server, tok.Value.AccessToken, paymentSignature: null, ct)
          .ConfigureAwait(false);
        if (unpaid.StatusCode != 402)
        {
          Terminal.WriteErrorLine(
            $"Expected HTTP 402 Payment Required; got {unpaid.StatusCode}. Body:".Red());
          Terminal.WriteErrorLine(unpaid.RawBody);
          if (unpaid.StatusCode == 503)
          {
            Terminal.WriteErrorLine(
              "503 means payment is disabled/misconfigured (never 402 on free routes). Enable MeteredCapabilityOptions in Development.");
          }

          Environment.ExitCode = 1;
          return Value;
        }

        Terminal.WriteLine("  HTTP 402 Payment Required".Green());
        if (!string.IsNullOrWhiteSpace(unpaid.PaymentRequiredHeader))
        {
          Terminal.WriteLine($"  {AgentHttpClient.PaymentRequiredHeader}: <base64 challenge present>");
          string? decoded = TryDecodeBase64Utf8(unpaid.PaymentRequiredHeader);
          if (decoded is not null)
          {
            Terminal.WriteLine("  decoded challenge (truncated):");
            Terminal.WriteLine($"    {Truncate(decoded, 240)}");
          }
        }

        Terminal.WriteLine(unpaid.RawBody);

        // ── 5. optional paid retry ─────────────────────────────────────────
        Step("5/6  paid retry — PAYMENT-SIGNATURE (live settle) or stop for mock CI path");
        if (string.IsNullOrWhiteSpace(command.PaymentSignature))
        {
          Terminal.WriteLine(
            "No --payment-signature supplied. Live settle needs a real x402 payload for the challenge above.");
          Terminal.WriteLine(
            "CI continuous proof (mock facilitator, no network): web-jaribu Money_Path_E2E_Register_Then_402_Then_Pay_Then_Prepaid_Quota.");
          Terminal.WriteLine("Re-run with a signature to complete the paid call:");
          Terminal.WriteLine(
            "  dotnet run tools/agent-identity-cli/agent.cs -- money-path --payment-signature <base64>");
          Terminal.WriteLine("Or curl the same bearer + header (see task 104-014 Results).");
        }
        else
        {
          MeteredHttpResult paid = await Http
            .GetMeteredAsync(server, tok.Value.AccessToken, command.PaymentSignature, ct)
            .ConfigureAwait(false);
          if (!paid.Success || paid.Value is null)
          {
            Terminal.WriteErrorLine($"Paid retry HTTP {paid.StatusCode}".Red());
            Terminal.WriteErrorLine(paid.RawBody);
            Environment.ExitCode = 1;
            return Value;
          }

          Terminal.WriteLine("  HTTP 200 — capability delivered".Green());
          Terminal.WriteLine($"  fundingSource {paid.Value.FundingSource}");
          Terminal.WriteLine($"  balanceAfter  {paid.Value.BalanceAfter}");
          if (!string.IsNullOrWhiteSpace(paid.PaymentResponseHeader))
          {
            Terminal.WriteLine($"  {AgentHttpClient.PaymentResponseHeader}: present");
          }

          Terminal.WriteLine(paid.RawBody);
        }

        // ── 6. whoami ──────────────────────────────────────────────────────
        Step("6/6  whoami — same opaque bearer; TrustTier becomes Funded after a successful settle");
        HttpResult<WhoAmIResponse> me = await Http
          .GetMeAsync(server, tok.Value.AccessToken, ct)
          .ConfigureAwait(false);
        if (!me.Success || me.Value is null)
        {
          return FailHttp(me.StatusCode, me.RawBody);
        }

        Terminal.WriteLine($"  PrincipalId {me.Value.PrincipalId}".Green());
        Terminal.WriteLine($"  Kind        {me.Value.Kind}".Green());
        Terminal.WriteLine($"  TrustTier   {me.Value.TrustTier}".Green());
        Terminal.WriteLine($"  Scopes      {string.Join(", ", me.Value.Scopes)}".Green());
        if (string.IsNullOrWhiteSpace(command.PaymentSignature))
        {
          Terminal.WriteLine("  (still Keyed until a settle promotes Funded — unpaid path only)");
        }

        Terminal.WriteLine();
        Terminal.WriteLine("Money-path narration complete.".Green());
        Terminal.WriteLine(
          "Quota for later prepaid calls is ledger balance after credit/settle, not a token claim.");
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
      Terminal.WriteLine(" Agent money path (task 104-014) — no Entra, no human sponsor");
      Terminal.WriteLine("══════════════════════════════════════════════════════════════");
      Terminal.WriteLine($"Server   : {server}");
      Terminal.WriteLine($"Key file : {Path.GetFullPath(keyFile)}");
      Terminal.WriteLine($"Metered  : GET {AgentHttpClient.MeteredCapabilityPath}");
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

    private static string? TryDecodeBase64Utf8(string headerValue)
    {
      try
      {
        byte[] bytes = Convert.FromBase64String(headerValue);
        return Encoding.UTF8.GetString(bytes);
      }
      catch (FormatException)
      {
        return null;
      }
    }

    private static string Truncate(string value, int max)
    {
      if (value.Length <= max)
      {
        return value;
      }

      return value[..max] + "…";
    }
  }
}
