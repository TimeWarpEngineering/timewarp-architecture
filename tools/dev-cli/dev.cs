#!/usr/bin/env -S dotnet --
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
// Thin wrapper around TimeWarp.Nuru to execute development commands.
// Shared endpoints (clean, self-install, check-version) come from TimeWarp.Nuru.DevCli.
// UseTelemetry is on so a CI job or a ganda walk that shells out to dev exports one
// command span. ServiceName is the literal timewarp-architecture-dev. An unset
// OTEL_EXPORTER_OTLP_ENDPOINT exports nothing.
#endregion

NuruApp app = NuruApp.CreateBuilder()
  .WithName("dev")
  .WithDescription("Development CLI for timewarp-architecture")
  .UseTelemetry(options => options.ServiceName = "timewarp-architecture-dev")
  .ConfigureServices(services =>
  {
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<NuGetVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
    services.AddSingleton<IPackableProjectService, PackableProjectService>();
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);