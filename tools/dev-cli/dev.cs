#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// DEV CLI - timewarp-architecture DEVELOPMENT TOOL
// ═══════════════════════════════════════════════════════════════════════════════
//
// Usage:
//   As runfile:  dotnet run tools/dev-cli/dev.cs -- <command>
//   As AOT:      ./bin/dev <command>
//
// Run `./bin/dev --help` for available commands.
//
// To bootstrap:
//   dotnet run tools/dev-cli/dev.cs -- self-install
//   direnv allow
//   dev --help
// ═══════════════════════════════════════════════════════════════════════════════

#region Purpose
// Entry point for the dev CLI
#endregion
#region Design
// Thin wrapper around TimeWarp.Nuru to execute development commands
#endregion

NuruApp app = NuruApp.CreateBuilder()
  .WithName("dev")
  .WithDescription("Development CLI for timewarp-architecture")
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
