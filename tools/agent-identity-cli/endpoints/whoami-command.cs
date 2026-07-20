#region Purpose
// Call GET /api/identity/agent/me with the stored bearer token.
#endregion
#region Design
// Proves end-to-end that the issued bearer is accepted by the identity:read policy.
// On non-2xx prints status + raw problem details body.
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("whoami", Description = "Call GET /api/identity/agent/me with the stored bearer")]
internal sealed class WhoamiCommand : ICommand<Unit>
{
  [Option("server", "s", Description = "Identity server base URL")]
  public string? Server { get; set; }

  [Option("key-file", "k", Description = "Path to the PKCS#8 PEM (used to locate the store sidecar)")]
  public string? KeyFile { get; set; }

  internal sealed class Handler : ICommandHandler<WhoamiCommand, Unit>
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

    public async ValueTask<Unit> Handle(WhoamiCommand command, CancellationToken ct)
    {
      string server = string.IsNullOrWhiteSpace(command.Server) ? PathDefaults.DefaultServer : command.Server;
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;

      try
      {
        AgentStoreRecord? record = Store.TryLoad(keyFile);
        if (record is null || string.IsNullOrWhiteSpace(record.AccessToken))
        {
          Terminal.WriteErrorLine($"No access token in store {PathDefaults.ResolveStorePath(keyFile)}. Run `token` first.".Red());
          Environment.ExitCode = 1;
          return Value;
        }

        Terminal.WriteLine($"GET {server}{AgentHttpClient.MePath}");
        HttpResult<WhoAmIResponse> result = await Http.GetMeAsync(server, record.AccessToken, ct).ConfigureAwait(false);
        if (!result.Success || result.Value is null)
        {
          Terminal.WriteErrorLine($"HTTP {result.StatusCode}".Red());
          Terminal.WriteErrorLine(result.RawBody);
          Environment.ExitCode = 1;
          return Value;
        }

        WhoAmIResponse me = result.Value;
        Terminal.WriteLine("Authenticated agent identity:".Green());
        Terminal.WriteLine($"PrincipalId : {me.PrincipalId}");
        Terminal.WriteLine($"Kind        : {me.Kind}");
        Terminal.WriteLine($"TrustTier   : {me.TrustTier}");
        Terminal.WriteLine($"Scopes      : {string.Join(", ", me.Scopes)}");
      }
      catch (Exception ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
      }

      return Value;
    }
  }
}
