#region Purpose
// Generate an ECDSA P-256 agent keypair, write PKCS#8 PEM, print SPKI base64url + KeyId.
#endregion
#region Design
// KeyId comes from AgentPublicKey.TryParse (library) so the printed id matches what the
// server will compute at registration time. Never prints private key material.
#endregion

namespace AgentIdentityCli.Commands;

[NuruRoute("keygen", Description = "Generate a P-256 agent keypair and write PKCS#8 PEM")]
internal sealed class KeygenCommand : ICommand<Unit>
{
  [Option("key-file", "k", Description = "Path for the PKCS#8 PEM private key")]
  public string? KeyFile { get; set; }

  [Option("force", "f", Description = "Overwrite an existing key file")]
  public bool Force { get; set; }

  internal sealed class Handler : ICommandHandler<KeygenCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly PathDefaults Paths;
    private readonly AgentSigning Signing;

    public Handler(ITerminal terminal, PathDefaults paths, AgentSigning signing)
    {
      Terminal = terminal;
      Paths = paths;
      Signing = signing;
    }

    public ValueTask<Unit> Handle(KeygenCommand command, CancellationToken ct)
    {
      string keyFile = string.IsNullOrWhiteSpace(command.KeyFile) ? Paths.DefaultKeyFilePath : command.KeyFile;

      try
      {
        GeneratedKey generated = Signing.GenerateKey();
        Signing.WriteKeyFile(keyFile, generated.Pem, command.Force);

        Terminal.WriteLine("Generated ECDSA P-256 agent key.".Green());
        Terminal.WriteLine($"Key file : {Path.GetFullPath(keyFile)}");
        Terminal.WriteLine($"SPKI     : {AgentSigning.ToBase64Url(generated.SpkiPublicKey)}");
        Terminal.WriteLine($"KeyId    : {AgentSigning.ToBase64Url(generated.KeyId)}");
        Terminal.WriteLine($"Store    : {PathDefaults.ResolveStorePath(keyFile)} (created on register/token)");
      }
      catch (Exception ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
      }

      return ValueTask.FromResult(Value);
    }
  }
}
