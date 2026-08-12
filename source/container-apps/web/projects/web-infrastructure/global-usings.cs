#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

// EF + FluentValidation only when postgres ships: without the flag, infrastructure is in-memory
// modules only (no entity configs), so these globals become IDE0005 (template smoke no-postgres).
// Monorepo dogfood: DefineConstants postgres in web-infrastructure.csproj (TWA0010).
// Template engine strips this block when the postgres flag is false.
#if(postgres)
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
#endif
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
// Solution usings
global using TimeWarp.Identity;
global using TimeWarp.Modules;
