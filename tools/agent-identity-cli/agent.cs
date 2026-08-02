#!/usr/bin/env -S dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// AGENT IDENTITY CLI - demo / reference client for agent key + token ceremony
// ═══════════════════════════════════════════════════════════════════════════════
//
// Usage:
//   As runfile:  dotnet run tools/agent-identity-cli/agent.cs -- <command>
//
// Commands: keygen | register | token | whoami | demo
// Default server: https://localhost:63611 (web-server launchSettings)
// ═══════════════════════════════════════════════════════════════════════════════

#region Purpose
// Entry point for the agent-identity demo CLI (task 104-029).
#endregion
#region Design
// TimeWarp.Nuru multi-file runfile mirroring tools/dev-cli/. ProjectReference to
// TimeWarp.Identity so signing uses AgentKeyProof.BuildSignedData (library pin) —
// never a private mirror of the signed-data construction. CLI-local wire DTOs only
// (no web-contracts dependency): this tool must remain a thin HTTP client that any
// agent author can copy without the template's contract assembly.
#endregion

// Runtime MS DI so service types can stay internal (Nuru source-gen DI requires public types).
// Internal services also keep compile-included helpers out of test discovery in the test project.
NuruApp app = NuruApp.CreateBuilder()
  .WithName("agent")
  .WithDescription("Agent identity demo CLI — keygen, register, token, whoami")
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddSingleton<PathDefaults>();
    services.AddSingleton<CliJson>();
    services.AddSingleton<LocalKeyStore>();
    services.AddSingleton<AgentHttpClient>();
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args).ConfigureAwait(false);
